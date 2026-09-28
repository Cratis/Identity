// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using MongoDB.Driver;

namespace Cratis.Identity.Storage;

/// <summary>
/// Represents an <see cref="IXmlRepository"/> that keeps the ASP.NET Core Data Protection key ring in MongoDB, so
/// session and antiforgery cookies survive restarts and are shared between replicas.
/// </summary>
/// <param name="database">The <see cref="IMongoDatabase"/>.</param>
public class MongoXmlRepository(IMongoDatabase database) : IXmlRepository
{
    /// <summary>
    /// The name of the collection keys are stored in.
    /// </summary>
    public const string CollectionName = "data-protection-keys";

    readonly IMongoCollection<DataProtectionKeyDocument> _collection = database.GetCollection<DataProtectionKeyDocument>(CollectionName);

    /// <inheritdoc/>
    public IReadOnlyCollection<XElement> GetAllElements() =>
        [.. _collection.Find(FilterDefinition<DataProtectionKeyDocument>.Empty).ToList().Select(_ => XElement.Parse(_.Xml))];

    /// <inheritdoc/>
    public void StoreElement(XElement element, string friendlyName) =>
        _collection.InsertOne(new DataProtectionKeyDocument { Id = friendlyName, Xml = element.ToString(SaveOptions.DisableFormatting) });
}
