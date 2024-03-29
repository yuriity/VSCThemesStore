using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using VSCThemesStore.WebApi.Domain.Models;
using VSCThemesStore.WebApi.Domain.Repositories;
using VSCThemesStore.WebApi.Controllers.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace VSCThemesStore.WebApi.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class GalleryController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IExtensionsMetadataRepository _metadataRepository;
        private readonly IThemeRepository _themeRepository;

        public GalleryController(
            IMapper mapper,
            IExtensionsMetadataRepository metadataRepository,
            IThemeRepository themeStoreRepository)
        {
            _mapper = mapper;
            _metadataRepository = metadataRepository;
            _themeRepository = themeStoreRepository;
        }

        // GET api/gallery?pageNumber=2&pageSize=10&sortBy=Downloads
        [HttpGet]
        public async Task<QueryResultResource<ExtensionMetadataResource>> Index(
            [FromQuery] StoreQueryResource queryResource)
        {
            var query = _mapper.Map<StoreQueryResource, StoreQuery>(queryResource);

            var queryResult = await _metadataRepository.GetItems(query);

            return _mapper.Map<QueryResult<ExtensionMetadata>, QueryResultResource<ExtensionMetadataResource>>(queryResult);
        }

        // GET api/gallery/id
        [HttpGet("{id}")]
        public async Task<ActionResult<ExtensionResource>> Get(string id)
        {
            var metadata = await _metadataRepository.GetExtensionMetadata(id);
            if (metadata == null)
            {
                Log.Information($"Couldn't find ExtensionMetadata '{id}'.");
                return new NotFoundResult();
            }
            if (metadata.Type != ExtensionType.Default)
            {
                Log.Information($"ExtensionMetadata '{id}' doesn't contain themes (ExtensionType: {metadata.Type})");
                return new NotFoundResult();
            }

            var storedTheme = await _themeRepository.GetTheme(id);
            if (storedTheme == null)
            {
                Log.Error($"Stored theme for '{id}' extension is empty.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            var extension = _mapper.Map<ExtensionMetadata, ExtensionResource>(metadata);
            extension.Themes = ConvertThemes(storedTheme.Themes);

            return new OkObjectResult(extension);
        }

        private List<ThemeResource> ConvertThemes(IEnumerable<Theme> storedThemes)
        {
            return storedThemes
                .Select(theme =>
                {
                    var themeResource = _mapper.Map<Theme, ThemeResource>(theme);
                    themeResource.TokenColors = theme.TokenColors
                        .Select(tc => _mapper.Map<TokenColor, TokenColorResource>(tc))
                        .ToList();
                    themeResource.Colors = new Dictionary<string, string>(
                        theme.Colors
                            .FindAll(c => !string.IsNullOrWhiteSpace(c.Value))
                            .Select(c => KeyValuePair.Create(c.PropertyName, c.Value)));

                    return themeResource;
                })
                .ToList();
        }
    }
}
