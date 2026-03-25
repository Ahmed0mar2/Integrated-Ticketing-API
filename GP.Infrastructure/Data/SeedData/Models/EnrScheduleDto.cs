using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GP.Infrastructure.Data.SeedData.Models
{
    public class EnrScheduleDto
    {
        [JsonPropertyName("train_number")]
        public string TrainNumber { get; set; } = null!;
        [JsonPropertyName("stops")]
        public List<EnrStopDto> Stops { get; set; } = new();
    }
}
