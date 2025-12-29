using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;

namespace Reservation.Models.ViewModels
{
    public class Record
    {
        public string creatOn { get; set; }
        public string restaurantName { get; set; }
        public string status { get; set; }
        public DateTime dateTime { get; set; }
        public int adults { get; set; }
        public int children { get; set; }
        public string note { get; set; }
    }
}