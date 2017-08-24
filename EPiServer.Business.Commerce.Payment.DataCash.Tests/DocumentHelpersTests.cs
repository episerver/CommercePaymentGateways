using DataCash;
using Xunit;

namespace EPiServer.Business.Commerce.Payment.DataCash.Tests
{
    public class DocumentHelpersTests
    {
        [Fact]
        public void CallGetResponseInfo_WhenDocumentNull_ShouldReturnNull()
        {
            var result = DocumentHelpers.GetResponseInfo(null, "SampleKey");

            Assert.Equal(null, result);
        }

        [InlineData("Response.sampleKey", "sample value")]
        [InlineData("Response.status", "1")]
        [InlineData("Response.information", "information")]
        [InlineData("Response.reason", "message")]
        [Theory]
        public void CallGetResponseInfo_WhenDocumentHasInformation_ShouldReturnValue(string key, string value)
        {
            var fakeResponseDoc = new Document();
            fakeResponseDoc.set(key, value);

            var result = DocumentHelpers.GetResponseInfo(fakeResponseDoc, key);

            Assert.Equal(value, result);
        }

        [Fact]
        public void CallIsSuccessful_WhenDocumentNull_ShouldReturnFalse()
        {
            var result = DocumentHelpers.IsSuccessful(null);

            Assert.Equal(false, result);
        }

        [Fact]
        public void CallIsSuccessful_WhenDocumentHasStatusEqual1_ShouldReturnTrue()
        {
            var fakeResponseDoc = new Document();
            fakeResponseDoc.set("Response.status", 1.ToString());

            var result = DocumentHelpers.IsSuccessful(fakeResponseDoc);

            Assert.Equal(true, result);
        }

        [Fact]
        public void CallIsSuccessful_WithDocumentHasStatusEqual0_ShouldReturnFalse()
        {
            var fakeResponseDoc = new Document();
            fakeResponseDoc.set("Response.status", 0.ToString());

            var result = DocumentHelpers.IsSuccessful(fakeResponseDoc);

            Assert.Equal(false, result);
        }

        [Fact]
        public void CallGetErrorMessage_WhenDocumentNull__ShouldReturnNull()
        {
            var result = DocumentHelpers.GetErrorMessage(null);

            Assert.Equal(null, result);
        }
        
        [InlineData("Response.information", "information", "information")]
        [InlineData("Response.reason", "message", "message")]
        [InlineData("Response.status", "1", "")]
        [InlineData("Response.sampleKey", "sample value", "")]
        [Theory]
        public void CallGetErrorMessage_WhenDocumentHasInformation_ShouldCorrectMessage(string key, string value, string exptected)
        {
            var fakeResponseDoc = new Document();
            fakeResponseDoc.set(key, value);

            var result = DocumentHelpers.GetErrorMessage(fakeResponseDoc);

            Assert.Equal(exptected, result);
        }
    }
}
