﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Netco.Logging;

namespace MagentoAccess.Services
{
	public class WebRequestServices : IWebRequestServices
	{
		#region BaseRequests
		public WebRequest CreateServiceGetRequest( string serviceUrl, Dictionary< string, string > rawUrlParameters )
		{
			var parametrizedServiceUrl = serviceUrl;

			if( rawUrlParameters.Any() )
			{
				parametrizedServiceUrl += "?" + rawUrlParameters.Keys.Aggregate( string.Empty,
					( accum, item ) => accum + "&" + string.Format( "{0}={1}", item, rawUrlParameters[ item ] ) );
			}

			var serviceRequest = ( HttpWebRequest )WebRequest.Create( parametrizedServiceUrl );
			serviceRequest.Method = WebRequestMethods.Http.Get;
			//
			serviceRequest.ContentType = "text/html";
			serviceRequest.KeepAlive = true;
			serviceRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			serviceRequest.CookieContainer = new CookieContainer();
			serviceRequest.CookieContainer.Add( new Uri( "http://192.168.0.104" ), new Cookie( "PHPSESSID", "mfl1c4qsrjs647chj2ummgo886" ) );
			serviceRequest.CookieContainer.Add( new Uri( "http://192.168.0.104" ), new Cookie( "adminhtml", "mk8rlurr9c4kaecnneakg55rv7" ) );
			serviceRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
			//
			return serviceRequest;
		}

		public async Task< WebRequest > CreateServiceGetRequestAsync( string serviceUrl, string body, Dictionary< string, string > rawHeaders )
		{
			try
			{
				var encoding = new UTF8Encoding();
				var encodedBody = encoding.GetBytes( body );

				var serviceRequest = ( HttpWebRequest )WebRequest.Create( serviceUrl );
				serviceRequest.Method = WebRequestMethods.Http.Get;
				serviceRequest.ContentType = "application/json";
				serviceRequest.ContentLength = encodedBody.Length;
				serviceRequest.KeepAlive = true;

				foreach( var rawHeadersKey in rawHeaders.Keys )
				{
					serviceRequest.Headers.Add( rawHeadersKey, rawHeaders[ rawHeadersKey ] );
				}

				using( var newStream = await serviceRequest.GetRequestStreamAsync().ConfigureAwait( false ) )
					newStream.Write( encodedBody, 0, encodedBody.Length );

				return serviceRequest;
			}
			catch( Exception )
			{
				throw;
			}
		}

		public void PopulateRequestByBody( string body, HttpWebRequest webRequest )
		{
			if( !string.IsNullOrWhiteSpace( body ) )
			{
				var encodedBody = new UTF8Encoding().GetBytes( body );

				webRequest.ContentLength = encodedBody.Length;
				webRequest.ContentType = "text/xml";
				var getRequestStremTask = webRequest.GetRequestStreamAsync();
				getRequestStremTask.Wait();
				using( var newStream = getRequestStremTask.Result )
					newStream.Write( encodedBody, 0, encodedBody.Length );
			}
		}
		#endregion

		#region ResponseHanding
		public Stream GetResponseStream( WebRequest webRequest )
		{
			this.LogTraceGetResponseStarted( webRequest );
			try
			{
				using( var response = ( HttpWebResponse )webRequest.GetResponse() )
				using( var dataStream = response.GetResponseStream() )
				{
					var memoryStream = new MemoryStream();
					if( dataStream != null )
						dataStream.CopyTo( memoryStream, 0x100 );
					memoryStream.Position = 0;
					this.LogTraceGetResponseEnded( webRequest, memoryStream );
					return memoryStream;
				}
			}
			catch
			{
				this.LogTraceGetResponseException( webRequest );
				throw;
			}
		}

		public async Task< Stream > GetResponseStreamAsync( WebRequest webRequest )
		{
			this.LogTraceGetResponseAsyncStarted( webRequest );
			try
			{
				using( var response = ( HttpWebResponse )await webRequest.GetResponseAsync().ConfigureAwait( false ) )
				using( var dataStream = await new TaskFactory< Stream >().StartNew( () => response != null ? response.GetResponseStream() : null ).ConfigureAwait( false ) )
				{
					var memoryStream = new MemoryStream();
					await dataStream.CopyToAsync( memoryStream, 0x100 ).ConfigureAwait( false );
					memoryStream.Position = 0;
					this.LogTraceGetResponseAsyncEnded( webRequest, memoryStream );
					return memoryStream;
				}
			}
			catch
			{
				this.LogTraceGetResponseAsyncException( webRequest );
				throw;
			}
		}
		#endregion

		#region logging
		private void LogTraceGetResponseStarted( WebRequest webRequest )
		{
			this.Log().Trace( "[magento] Get response url:{0} started.", webRequest.RequestUri );
		}

		private void LogTraceGetResponseEnded( WebRequest webRequest, Stream webResponseStream )
		{
			using( Stream streamCopy = new MemoryStream( ( int )webResponseStream.Length ) )
			{
				var sourcePos = webResponseStream.Position;
				webResponseStream.CopyTo( streamCopy );
				webResponseStream.Position = sourcePos;
				streamCopy.Position = 0;

				var responseStr = new StreamReader( streamCopy ).ReadToEnd();
				this.Log().Trace( "[magento] Get response url:{0} ended with {1}.", webRequest.RequestUri, responseStr );
			}
		}

		private void LogTraceGetResponseAsyncStarted( WebRequest webRequest )
		{
			this.Log().Trace( "[magento] Get response async url:{0} started.", webRequest.RequestUri );
		}

		private void LogTraceGetResponseAsyncEnded( WebRequest webRequest, Stream webResponseStream )
		{
			using( Stream streamCopy = new MemoryStream( ( int )webResponseStream.Length ) )
			{
				var sourcePos = webResponseStream.Position;
				webResponseStream.CopyTo( streamCopy );
				webResponseStream.Position = sourcePos;
				streamCopy.Position = 0;

				var responseStr = new StreamReader( streamCopy ).ReadToEnd();
				this.Log().Trace( "[magento] Get response async url:{0} ended with {1}.", webRequest.RequestUri, responseStr );
			}
		}

		private void LogTraceGetResponseException( WebRequest webRequest )
		{
			this.Log().Trace( "[magento] Get response url:{0} throw an exception .", webRequest.RequestUri );
		}

		private void LogTraceGetResponseAsyncException( WebRequest webRequest )
		{
			this.Log().Trace( "[magento] Get response async url:{0} throw an exception .", webRequest.RequestUri );
		}
		#endregion
	}
}