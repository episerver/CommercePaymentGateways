using System;
using DataCash;

namespace EPiServer.Business.Commerce.Payment.DataCash
{
    public static class DocumentHelpers
    {
        public static Document SendTransaction(Document documentToSend, Config dataCashConfiguration)
        {
            return new Agent(dataCashConfiguration).send(documentToSend);
        }

        public static string GetResponseInfo(Document responseDoc, string infoKey)
        {
            return responseDoc?.get(infoKey);
        }

        public static string GetErrorMessage(Document responseDoc)
        {
            var message = GetResponseInfo(responseDoc, "Response.information");
            if (String.IsNullOrEmpty(message))
            {
                message = GetResponseInfo(responseDoc, "Response.reason");
            }
            return message;
        }

        public static bool IsSuccessful(Document responseDoc)
        {
            var value = GetResponseInfo(responseDoc, "Response.status");
            return value == null ? false : Int32.Parse(value) == 1;
        }

    }
}