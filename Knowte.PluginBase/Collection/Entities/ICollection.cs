﻿namespace Knowte.PluginBase.Collection.Entities
{
    public interface ICollection
    {
        string Id { get; set; }

        string Title { get; set; }

        long IsActive { get; set; }
    }
}
