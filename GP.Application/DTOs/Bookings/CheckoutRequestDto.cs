using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GP.Application.DTOs.Bookings
{
    public class CheckoutRequestDto
    {
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
