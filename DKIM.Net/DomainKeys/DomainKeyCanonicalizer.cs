/*
 * DKIM.Net
 * 
 * Copyright (C) 2011 Damien McGivern, damien@mcgiv.com
 * 
 * 
 * 
 * */
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace DKIM
{
	public enum DomainKeyCanonicalizationAlgorithm
	{
		Simple,

		/// <summary>
		/// No Folding White Space
		/// </summary>
		Nofws
	}

	public static class DomainKeyCanonicalizer
	{
		public static string Canonicalize(Email email, DomainKeyCanonicalizationAlgorithm algorithm, List<EmailHeader> headersToSign)
		{

			Func<String, string> process;
			switch (algorithm)
			{
				case DomainKeyCanonicalizationAlgorithm.Simple:
					{
						process = x => x;
						break;
					}
				case DomainKeyCanonicalizationAlgorithm.Nofws:
					{
						process = x => x.RemoveWhitespace();
						break;
					}
				default:
					{
						throw new ArgumentException("Invalid canonicalization type.");
					}
			}

			var headers = new StringBuilder();
			foreach (var h in headersToSign)
			{
				headers.Append(process(h.Key));
				headers.Append(':');
				headers.Append(process(" " + h.Value));
				headers.Append(Email.NewLine);
			}


			var body = new StringBuilder();
			using (var reader = new StringReader(email.Body))
			{
				string line;
				int emptyLines = 0;

				// if only empty lines don't write until these is text after them
				while ((line = reader.ReadLine()) != null)
				{
					if (line.Length == 0)
					{
						emptyLines++;
					}
					else
					{
						while (emptyLines > 0)
						{
							body.Append(Email.NewLine);
							emptyLines--;
						}

						body.Append(process(line));
						body.Append(Email.NewLine);

					}

				}
			}


			// If the body consists entirely of empty lines, then the header/body
			// line is similarly ignored.
			if (body.Length == 0)
			{
				return headers.ToString();
			}

			
			headers.Append(Email.NewLine);// header/body seperator line
			headers.Append(body);
			return headers.ToString();
		}
	}
}
