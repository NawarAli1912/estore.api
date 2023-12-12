﻿using Application.Common.Data;
using Domain.ModelsSnapshots;
using Microsoft.EntityFrameworkCore;
using Nest;
using Quartz;

namespace Infrastructure.BackgroundJobs;

public sealed class ElasticSearchSync(IApplicationDbContext context, IElasticClient elasticClient) : IJob
{
    private readonly IApplicationDbContext _context = context;
    private readonly IElasticClient _elasticClient = elasticClient;

    public async Task Execute(IJobExecutionContext context)
    {
        var dbProductsDict = await _context
                .Products
                .Select(p => ProductSnapshot.Snapshot(p))
                .ToDictionaryAsync(p => p.Id, p => p);


        var scrollResponse = _elasticClient.Search<ProductSnapshot>(s => s
            .Scroll("5m") // Set the scroll timeout
            .Size(500)   // Set the batch size
        );

        var elasticProducts = new List<ProductSnapshot>();
        while (scrollResponse.IsValid && scrollResponse.Documents.Count != 0)
        {
            // Process each batch of documents
            foreach (var productSnapshot in scrollResponse.Documents)
            {
                elasticProducts.Add(productSnapshot);
            }

            // Fetch the next batch
            scrollResponse = _elasticClient.Scroll<ProductSnapshot>("5m", scrollResponse.ScrollId);
        }

        var elasticProductsDict = elasticProducts.ToDictionary(p => p.Id, p => p);

        List<ProductSnapshot> toUpdate = [];
        List<ProductSnapshot> toDelete = [];
        List<ProductSnapshot> toAdd = [];
        foreach (var item in dbProductsDict.Values)
        {
            if (elasticProductsDict.TryGetValue(item.Id, out var elasticProduct))
            {
                if (!ProductSnapshot.Equals(item, elasticProduct))
                {
                    toUpdate.Add(item);
                }
            }
            else
            {
                toAdd.Add(item);
            }
        }

        foreach (var elasticProduct in elasticProductsDict.Values)
        {
            if (!dbProductsDict.ContainsKey(elasticProduct.Id))
            {
                toDelete.Add(elasticProduct);
            }
        }

        foreach (var item in toUpdate)
        {
            await _elasticClient.UpdateAsync<ProductSnapshot>(item.Id, u => u
                    .Doc(item)
                    .Refresh(Elasticsearch.Net.Refresh.True));
        }

        foreach (var item in toDelete)
        {
            await _elasticClient.DeleteAsync<ProductSnapshot>(item.Id);
        }

        foreach (var item in toAdd)
        {
            await _elasticClient.IndexDocumentAsync(item);
        }
    }
}
