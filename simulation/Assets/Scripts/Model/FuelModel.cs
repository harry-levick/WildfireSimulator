using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class FuelModel
{
    public string code;
    public int number;
    public string name;
    public string description;
    public List<float> fuel_load;
    public string type;
    public List<int> sav_ratio;
    public float fuel_bed_depth;
    public float dead_fuel_moisture_of_extinction;
    public int characteristic_sav;
    public float bulk_density;
    public float relative_packing_ratio;
}

