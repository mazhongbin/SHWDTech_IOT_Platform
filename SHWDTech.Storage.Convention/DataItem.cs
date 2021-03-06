﻿using System;
using System.ComponentModel.DataAnnotations;

namespace SHWDTech.IOT.Storage.Convention
{
    [Serializable]
    public class DataItem<T> : IDataItem<T>
    {
        [Key]
        public T Id { get; set; }
    }
}
