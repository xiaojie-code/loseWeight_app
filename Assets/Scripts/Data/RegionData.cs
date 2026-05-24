using System;
using System.Collections.Generic;

namespace LoseWeight.Data
{
    /// <summary>
    /// 地区数据 - 省市二级联动
    /// </summary>
    [Serializable]
    public class RegionDatabase
    {
        public List<ProvinceData> Provinces;
    }

    [Serializable]
    public class ProvinceData
    {
        public string Name;
        public string Code;
        public List<CityData> Cities;
    }

    [Serializable]
    public class CityData
    {
        public string Name;
        public string Code;
    }
}
