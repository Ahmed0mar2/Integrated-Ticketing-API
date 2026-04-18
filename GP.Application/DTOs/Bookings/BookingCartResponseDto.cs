using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.DTOs.Bookings
{
    public class BookingCartResponseDto
    {
        public List<CartItemDto> Items { get; set; } = [];
        public decimal GrandTotal { get; set; }
    }
}
