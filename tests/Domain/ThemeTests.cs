using VSCThemesStore.WebApi.Domain.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace VSCThemesStore.WebApi.Tests.Domain
{
    public class ThemeTests
    {
        [Fact]
        public void ParseColors_SimpleValidJObject_ReturnsCorrectResult()
        {
            const string jsonString = @"{
                'colors': {
                    'activityBar.background': '#ff0000'
                }
            }";

            var result = Theme.ParseColors(JObject.Parse(jsonString));

            Assert.Equal("activityBar.background", result[0].PropertyName);
            Assert.Equal("#ff0000", result[0].Value);
        }

        [Theory]
        [InlineData("{ 'colors': { 'activityBar.background': null } }")]
        [InlineData("{ 'colors': { 'activityBar.background': '' } }")]
        [InlineData("{ 'colors': { '': '#ff0000' } }")]
        public void ParseColors_KeyOrValueIsEmpty_ShouldNotReturnAnything(string jsonString)
        {
            var colors = Theme.ParseColors(JObject.Parse(jsonString));

            Assert.Empty(colors);
        }

        [Fact]
        public void ParseTokenColors_TokenColorWithOnlyRequiredProperies_ReturnsCorrectResult()
        {
            const string jsonString = @"{
                'tokenColors': [{
                        'scope': 'tokenColor_scope',
                        'settings': {
                            'foreground': 'settings_foreground'
                        }
                    }
                ]
            }";

            var result = Theme.ParseTokenColors(JObject.Parse(jsonString));

            Assert.Equal("tokenColor_scope", result[0].Scope);
            Assert.Equal("settings_foreground", result[0].Settings.Foreground);
        }

        [Theory]
        // Scope is empty
        [InlineData("{'tokenColors':[{'name':'tokenColor_name','settings':{'foreground':'settings_foreground','fontStyle':'settings_fontStyle'}}]}")]
        // Settings is empty
        [InlineData("{'tokenColors':[{'name':'tokenColor_name','scope':'tokenColor_scope'}]}")]
        public void ParseTokenColors_TokenColorWithInvalidProperies_ShouldNotReturnThisTokenColors(string jsonString)
        {
            var result = Theme.ParseTokenColors(JObject.Parse(jsonString));

            Assert.Empty(result);
        }

        [Theory]
        [InlineData("{'tokenColors':[{'scope':'scope1,scope2,scope3','settings':{'foreground':'settings_foreground'}}]}")]
        [InlineData("{'tokenColors':[{'scope':['scope1','scope2','scope3'],'settings':{'foreground':'settings_foreground'}}]}")]
        public void ParseTokenColors_JObjectWithValidScope_ReturnsCorrectResult(string jsonString)
        {
            var result = Theme.ParseTokenColors(JObject.Parse(jsonString));

            Assert.Single(result, token => token.Scope == "scope1,scope2,scope3");
        }
    }
}
