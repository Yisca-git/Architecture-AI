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

namespace Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CategoryService> _logger;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _categoryTTL;

        public CategoryService(
            ICategoryRepository categoryRepository,
            IMapper mapper,
            ICacheService cacheService,
            ILogger<CategoryService> logger,
            IConfiguration configuration)
        {
            _mapper = mapper;
            _categoryRepository = categoryRepository;
            _cacheService = cacheService;
            _logger = logger;
            _configuration = configuration;
            _categoryTTL = TimeSpan.FromSeconds(configuration.GetSection("Redis:CategoryTTL").Get<int?>() ?? 3600);
        }

        public async Task<bool> IsExistsCategoryById(int id)
        {
            return await _categoryRepository.IsExistsCategoryById(id);  
        }
        public async Task<List<CategoryDTO>> GetCategories()
        {
            const string cacheKey = "categories:all";

            // Try to get from cache first
            var cachedCategories = await _cacheService.GetAsync<List<CategoryDTO>>(cacheKey);
            if (cachedCategories != null)
            {
                _logger.LogInformation("Cache HIT - Returning {Count} categories from cache", cachedCategories.Count);
                return cachedCategories;
            }

            // Cache miss - fetch from database
            _logger.LogInformation("Cache MISS - Fetching categories from database");
            List<Category> categories = await _categoryRepository.GetCategories();
            List<CategoryDTO> categoriesDTO = _mapper.Map<List<Category>, List<CategoryDTO>>(categories);

            // Store in cache
            await _cacheService.SetAsync(cacheKey, categoriesDTO, _categoryTTL);
            _logger.LogInformation("Cache STORED - Cached {Count} categories with TTL of {TTL} seconds", categoriesDTO.Count, _categoryTTL.TotalSeconds);

            return categoriesDTO;
        }
        public async Task<CategoryDTO> GetCategoryId(int id)
        {
            Category? category = await _categoryRepository.GetCategoryById(id);
            if (category == null)
                return null;
            CategoryDTO categoryDTO = _mapper.Map<Category, CategoryDTO>(category);
            return categoryDTO;
        }
        public async Task<CategoryDTO> AddCategory(NewCategoryDTO newCategory)
        {
            Category category = _mapper.Map<NewCategoryDTO, Category>(newCategory);
            Category addedCategory = await _categoryRepository.AddCategory(category);
            CategoryDTO categoryDTO = _mapper.Map<Category, CategoryDTO>(addedCategory);

            // Invalidate categories cache since we added a new category
            await _cacheService.RemoveAsync("categories:all");
            _logger.LogInformation("Invalidated categories cache after adding new category: {CategoryName}", categoryDTO.Name);

            return categoryDTO;
        }
       
    }
}
