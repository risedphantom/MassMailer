/*
 * DKIM.Net
 * 
 * Copyright (C) 2011 Damien McGivern, damien@mcgiv.com
 * 
 * 
 * 
 * */
using System;
using System.Net.Mail;
using System.Text;
using System.Collections.Generic;

namespace DKIM
{
	public class DomainKeySigner
	{
		/// <summary>
		/// Header key used to add DKIM information to email.
		/// </summary>
		public const string SignatureKey = "DomainKey-Signature";


		private readonly IPrivateKeySigner _privateKeySigner;


		/// <summary>
		/// The domain that will be signing the email.
		/// </summary>
		private readonly string _domain;

		/// <summary>
		/// The selector used to obtain the public key.
		/// </summary>
		private readonly string _selector;


		/// <summary>
		/// Be careful what headers you sign. Ensure that they are not changed by your SMTP server or relay.
		/// If a header if changed after signing DKIM will fail.
		/// </summary>
		private readonly string[] _headersToSign;

		public DomainKeyCanonicalizationAlgorithm Canonicalization { get; set; }


		public DomainKeySigner(IPrivateKeySigner privateKeySigner, string domain, string selector, string[] headersToSign)
		{
			if (privateKeySigner == null)
			{
				throw new ArgumentNullException("privateKeySigner");
			}

			if (domain == null)
			{
				throw new ArgumentNullException("domain");
			}

			if (selector == null)
			{
				throw new ArgumentNullException("selector");
			}


			_domain = domain;
			_selector = selector;
			_headersToSign = headersToSign;
			_privateKeySigner = privateKeySigner.EnsureRsaSha1();
		}


		public EmailHeader SignMessage(Email email)
		{
			var signatureValue = new StringBuilder();


			// algorithm used
			signatureValue.Append("a=");
			signatureValue.Append(_privateKeySigner.Algorithm);
			signatureValue.Append("; ");


			// Canonicalization
			signatureValue.Append("c=");
			signatureValue.Append(this.Canonicalization.ToString().ToLower());
			signatureValue.Append("; ");


			// signing domain
			signatureValue.Append("d=");
			signatureValue.Append(_domain);
			signatureValue.Append("; ");


			// headers to be signed
			var headers = email.GetHeadersToSign(_headersToSign);
			if (headers.Count>0)
			{
				signatureValue.Append("h=");
				foreach (var header in headers)
				{
					signatureValue.Append(header.Key);
					signatureValue.Append(':');
				}
				signatureValue.Length--;
				signatureValue.Append("; ");
			}


			// public key location
			signatureValue.Append("q=dns; ");


			// selector
			signatureValue.Append("s=");
			signatureValue.Append(_selector);
			signatureValue.Append("; ");


			// signature data
			signatureValue.Append("b=");
			signatureValue.Append(SignSignature(email, headers));
			signatureValue.Append(";");

			return new EmailHeader() { Key = SignatureKey, Value = signatureValue.ToString() };
		}


		public string SignSignature(Email email, List<EmailHeader> headers)
		{
			var text = DomainKeyCanonicalizer.Canonicalize(email, this.Canonicalization, headers);
			return Convert.ToBase64String(_privateKeySigner.Sign(email.Encoding.GetBytes(text)));
		}
	}
}
