/*
 * DKIM.Net
 * 
 * Copyright (C) 2011 Damien McGivern, damien@mcgiv.com
 * 
 * 
 * 
 * */
using System;
using System.Text;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace DKIM
{
	public class DkimSigner
	{
		/// <summary>
		/// Header key used to add DKIM information to email.
		/// </summary>
		public const string SignatureKey = "DKIM-Signature";

		private readonly IPrivateKeySigner _privateKeySigner;

		/// <summary>
		/// The domain that will be signing the email.
		/// </summary>
		private readonly string _domain;

		/// <summary>
		/// The selector used to obtain the public key.
		/// see http://www.dkim.org/info/dkim-faq.html#technical
		/// </summary>
		private readonly string _selector;

		private readonly string[] _headersToSign;
        
		public DkimSigner(IPrivateKeySigner privateKeySigner, string domain, string selector, string[] headersToSign = null)
		{
			if (privateKeySigner == null)
				throw new ArgumentNullException("privateKeySigner");

			if (domain == null)
				throw new ArgumentNullException("domain");

			if (selector == null)
				throw new ArgumentNullException("selector");


			_privateKeySigner = privateKeySigner;
			_domain = domain;
			_selector = selector;
			_headersToSign = headersToSign;
		}
		
		public DkimCanonicalizationAlgorithm HeaderCanonicalization { get; set; }
		public DkimCanonicalizationAlgorithm BodyCanonicalization { get; set; }
        
		public EmailHeader SignMessage(Email email)
		{
			// Find the actual headers we're going to sign
			var headers = email.GetHeadersToSign(_headersToSign);

			// Generate the header value
			var value = GenerateDkimHeaderValue(email, headers);

			// Add the signature key
			headers.Add(new EmailHeader
			{
			    Key = SignatureKey, 
                Value = value
			});

			// sign email
			value += GenerateSignature(email, headers);

			// Return the new header
			return new EmailHeader
			{
			    Key = SignatureKey, 
                Value = value
			};
		}
        
		/*
		 * see http://www.dkim.org/specs/rfc4871-dkimbase.html#dkim-sig-hdr
		 * 
		 * */
		public string GenerateDkimHeaderValue(Email email, List<EmailHeader> headers)
		{
			// timestamp  - seconds since 00:00:00 on January 1, 1970 UTC
			var t = DateTime.Now.ToUniversalTime() - DateTime.SpecifyKind(DateTime.Parse("00:00:00 January 1, 1970"), DateTimeKind.Utc);
            
			var signatureValue = new StringBuilder();

			const string start = " ";
			const string end = ";";
			
			signatureValue.Append("v=1;");
			
			// algorithm used
			signatureValue.Append(start);
			signatureValue.Append("a=");
			signatureValue.Append(_privateKeySigner.Algorithm);
			signatureValue.Append(end);

			// Canonicalization
			signatureValue.Append(start);
			signatureValue.Append("c=");
			signatureValue.Append(HeaderCanonicalization.ToString().ToLower());
			signatureValue.Append('/');
			signatureValue.Append(BodyCanonicalization.ToString().ToLower());
			signatureValue.Append(end);

			// signing domain
			signatureValue.Append(start);
			signatureValue.Append("d=");
			signatureValue.Append(_domain);
			signatureValue.Append(end);

			// headers to be signed
			signatureValue.Append(start);
			signatureValue.Append("h=");
			foreach (var header in headers)
			{
				signatureValue.Append(header.Key);
				signatureValue.Append(':');
			}
			signatureValue.Length--;
			signatureValue.Append(end);

			// i=identity
			// not supported

			// l=body length
			//not supported

			// public key location
			signatureValue.Append(start);
			signatureValue.Append("q=dns/txt");
			signatureValue.Append(end);

			// selector
			signatureValue.Append(start);
			signatureValue.Append("s=");
			signatureValue.Append(_selector);
			signatureValue.Append(end);

			// time sent
			signatureValue.Append(start);
			signatureValue.Append("t=");
			signatureValue.Append((int)t.TotalSeconds);
			signatureValue.Append(end);

			// x=expiration
			// not supported

			// hash of body
			signatureValue.Append(start);
			signatureValue.Append("bh=");
			signatureValue.Append(SignBody(email));
			signatureValue.Append(end);

			// x=copied header fields
			// not supported

			signatureValue.Append(start);
			signatureValue.Append("b=");
			
			return signatureValue.ToString();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="email">The email to sign.</param>
        /// <param name="headers"></param>
		/// <returns></returns>
		public string GenerateSignature(Email email, List<EmailHeader> headers)
		{
			var cheaders = DkimCanonicalizer.CanonicalizeHeaders(headers, HeaderCanonicalization);
			return Convert.ToBase64String(_privateKeySigner.Sign(email.Encoding.GetBytes(cheaders)));
		}

		public string SignBody(Email email)
		{
			var cb = DkimCanonicalizer.CanonicalizeBody(email.Body, BodyCanonicalization);
			return Convert.ToBase64String(_privateKeySigner.Hash(email.Encoding.GetBytes(cb)));
		}

	}
}
