using AutoMapper;
using Entities;
using DTOs;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text.Json;

namespace Services
{
    public class ModelService : IModelService
    {
        private readonly IModelRepository _modelRepository;
        private readonly IDressService _dressService;
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ModelService> _logger;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _modelTTL;

        public ModelService(
            IModelRepository modelRepository,
            IMapper mapper,
            IDressService dressService,
            ICategoryService categoryService,
            ICacheService cacheService,
            ILogger<ModelService> logger,
            IConfiguration configuration)
        {
            _mapper = mapper;
            _dressService = dressService;
            _modelRepository = modelRepository;
            _categoryService = categoryService;
            _cacheService = cacheService;
            _logger = logger;
            _configuration = configuration;
            _modelTTL = TimeSpan.FromSeconds(configuration.GetSection("Redis:ModelTTL").Get<int?>() ?? 900);
        }
        public async Task<bool> IsExistsModelById(int id)
        {
            return await _modelRepository.IsExistsModelById(id);
        }
        public async Task<bool> checkCategories(List<int> categories)
        {
            for (int i = 0; i < categories.Count(); i++) {
                if (!await _categoryService.IsExistsCategoryById(categories[i]))
                    return false;
            }

            //foreach (var category in categories)
            //{
            //    if(!await _categoryService.IsExistsCategoryById(category.Id))
            //        return false;
            //}
            return true;
        }
        public bool checkPrice(int price)
        {
            return price > 0;
        }
        public bool ValidateQueryParameters(int position, int skip, int? minPrice, int? maxPrice)
        {
            if(minPrice.HasValue && maxPrice.HasValue)
                return position >= 0 && skip >= 0 && minPrice < maxPrice;
            return position >= 0 && skip >= 0;
        }
        public async Task<ModelDTO> GetModelById(int id)
        {
            var cacheKey = $"model:{id}";

            // Try to get from cache first
            var cachedModel = await _cacheService.GetAsync<ModelDTO>(cacheKey);
            if (cachedModel != null)
            {
                _logger.LogInformation("Returning model {ModelId} from cache", id);
                return cachedModel;
            }

            // Cache miss - fetch from database
            _logger.LogInformation("Cache miss - fetching model {ModelId} from database", id);
            Model? model = await _modelRepository.GetModelById(id);
            if (model == null)
                return null;
            
            ModelDTO modelDTO = _mapper.Map<Model, ModelDTO>(model);

            // Store in cache
            await _cacheService.SetAsync(cacheKey, modelDTO, _modelTTL);
            _logger.LogInformation("Cached model {ModelId} with TTL of {TTL} seconds", id, _modelTTL.TotalSeconds);

            return modelDTO;
        }
        public async Task<FinalModels> GetModelds(string? description, int? minPrice, int? maxPrice,
            int[] categoriesId, string[] colors, int position = 1, int skip = 8)
        {
            // Generate cache key based on query parameters
            var cacheKey = GenerateModelsQueryCacheKey(description, minPrice, maxPrice, categoriesId, colors, position, skip);

            // Try to get from cache first
            var cachedModels = await _cacheService.GetAsync<FinalModels>(cacheKey);
            if (cachedModels != null)
            {
                _logger.LogInformation("Returning filtered models from cache (key: {CacheKey})", cacheKey);
                return cachedModels;
            }

            // Cache miss - fetch from database
            _logger.LogInformation("Cache miss - fetching filtered models from database (key: {CacheKey})", cacheKey);
            (List<Model> Items, int TotalCount) products = await _modelRepository
                        .GetModels(description, minPrice, maxPrice, categoriesId, colors, position, skip);
            List<ModelDTO> productsDTO = _mapper.Map<List<Model>, List<ModelDTO>>(products.Items);
            bool hasNext = (products.TotalCount - (position * skip)) > 0;
            bool hasPrev = position > 1;
            FinalModels finalProducts = new()
            {
                Items = productsDTO,
                TotalCount = products.TotalCount,
                HasNext = hasNext,
                HasPrev = hasPrev
            };

            // Store in cache
            await _cacheService.SetAsync(cacheKey, finalProducts, _modelTTL);
            _logger.LogInformation("Cached filtered models with TTL of {TTL} seconds (key: {CacheKey})", _modelTTL.TotalSeconds, cacheKey);

            return finalProducts;
        }

        private string GenerateModelsQueryCacheKey(string? description, int? minPrice, int? maxPrice,
            int[] categoriesId, string[] colors, int position, int skip)
        {
            // Create a unique cache key based on all query parameters
            var keyData = $"models:query:{description ?? "null"}:{minPrice?.ToString() ?? "null"}:{maxPrice?.ToString() ?? "null"}:" +
                          $"{string.Join(",", categoriesId ?? Array.Empty<int>())}:{string.Join(",", colors ?? Array.Empty<string>())}:" +
                          $"{position}:{skip}";
            
            // Hash the key to keep it shorter (optional but recommended for long keys)
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyData));
            var hash = Convert.ToBase64String(hashBytes).Replace("+", "-").Replace("/", "_").Substring(0, 16);
            
            return $"models:filter:{hash}";
        }
        public async Task<ModelDTO> AddModel(NewModelDTO newModel)
        {
            Model addedModel = _mapper.Map<NewModelDTO, Model>(newModel);
            addedModel.IsActive = true;
            Model model = await _modelRepository.AddModel(addedModel);
            ModelDTO modelDTO = _mapper.Map<Model, ModelDTO>(model);

            // Invalidate all models filter caches since a new model was added
            await InvalidateModelsCaches();
            _logger.LogInformation("Invalidated models caches after adding new model: {ModelName}", modelDTO.Name);

            return modelDTO;
        }
        public async Task UpdateModel(int id, NewModelDTO updateModel)
        {
            Model update = _mapper.Map<NewModelDTO, Model>(updateModel);
            update.Id = id;
            update.IsActive = true;
            await _modelRepository.UpdateModel(update);

            // Invalidate specific model cache and all filter caches
            await _cacheService.RemoveAsync($"model:{id}");
            await InvalidateModelsCaches();
            _logger.LogInformation("Invalidated model {ModelId} cache and filter caches after update", id);
        }
        public async Task DeleteModel(int id)
        {
            Model model = await _modelRepository.GetModelById(id);
            foreach (var dress in model.Dresses)
            {
                DressDTO dressDTO = _mapper.Map<Dress, DressDTO>(dress);
                await _dressService.DeleteDress(dress.Id);
            }
            await _modelRepository.DeleteModel(id);

            // Invalidate specific model cache and all filter caches
            await _cacheService.RemoveAsync($"model:{id}");
            await InvalidateModelsCaches();
            _logger.LogInformation("Invalidated model {ModelId} cache and filter caches after deletion", id);
        }

        private async Task InvalidateModelsCaches()
        {
            // Remove all cached model filter queries
            await _cacheService.RemoveByPatternAsync("models:filter:*");
        }
    }
}
