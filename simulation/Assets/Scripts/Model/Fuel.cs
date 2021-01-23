using System;
using System.Collections.Generic;
using System.Linq;

namespace Player.Model
{
    [Serializable]
    public class Fuel
    {
        public string code;
        public int number;
        public string name;
        public string description;
        public List<double> fuel_load;
        public string type;
        public List<int> sav_ratio;
        public double fuel_bed_depth;
        public double dead_fuel_moisture_of_extinction;
        public int characteristic_sav;
        public double bulk_density;
        public double relative_packing_ratio;
        public double optimum_packing_ratio {
            get
            {
                if (characteristic_sav == 0) { return 0; }
                else { return 3.348 * Math.Pow(characteristic_sav, -0.8189); }
            }
        }
        public double packing_ratio => relative_packing_ratio * optimum_packing_ratio;
        public const int heat_content = 8000; // constant
        public const double total_mineral_content = 0.0555; // constant
        public const double effective_mineral_content = 0.01; // constant
        public double oven_dry_fuel_load => fuel_bed_depth * bulk_density;
        public const double particle_density = 32; // constant
        public double mean_bulk_density {
            get
            {
                if (fuel_bed_depth == 0) { return 0; }
                else
                {
                    return (1 / fuel_bed_depth) * fuel_load.Sum();
                }

            }
        }
    }

}
