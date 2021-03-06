using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Contracts;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.Entities;
using Play.Common;
namespace Play.Catalog.Service.Controllers
{
    // https://localhost:5001/items
    [ApiController]
    [Route("items")]
    public class ItemController : ControllerBase
    {
        private readonly IRepository<Item> _itemsRepository;
        private readonly IPublishEndpoint _publishEndPoint;

        public ItemController(IRepository<Item> itemsRepository, IPublishEndpoint publishEndPoint)
        {
            this._itemsRepository = itemsRepository;
            this._publishEndPoint = publishEndPoint;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
        {
            try
            {
                var items = (await _itemsRepository.GetAllAsync())
                                            .Select(x => x.AsDto());
                return Ok(items);
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
            }
            return null;
        }

        // GET /items/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            var item = await _itemsRepository.GetAsync(id);
            if (item == null)
                return NotFound();

            return item.AsDto();
        }

        // POST /items
        [HttpPost]
        public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto createItemDto)
        {
            var item = new Item
            {
                Name = createItemDto.Name,
                Descriptipn = createItemDto.Description,
                Price = createItemDto.Price,
                CreatedDate = DateTimeOffset.UtcNow
            };

            await _itemsRepository.CreateAsync(item);

            await _publishEndPoint.Publish(new CatalogItemCreated(item.Id, item.Name, item.Descriptipn));

            return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id }, item);
        }

        // PUT /items/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
        {
            var existingItem = await _itemsRepository.GetAsync(id);
            if (existingItem == null)
                return NotFound();

            existingItem.Name = updateItemDto.Name;
            existingItem.Descriptipn = updateItemDto.Description;
            existingItem.Price = updateItemDto.Price;

            await _itemsRepository.UpdateAsync(existingItem);

            await _publishEndPoint.Publish(new CatalogItemUpdated(existingItem.Id, existingItem.Name, existingItem.Descriptipn));

            return NoContent();
        }

        // DELETE /items/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var item = await _itemsRepository.GetAsync(id);

            if (item == null)
                return NotFound();

            await _itemsRepository.RemoveAsync(item.Id);

            await _publishEndPoint.Publish(new CatalogItemDeleted(id));

            return NoContent();
        }
    }
}