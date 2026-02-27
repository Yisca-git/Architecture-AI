using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public record NewDressDTO
    (
        [Required]
        int ModelId,
        [Required]
        string Size,
        [Required]
        int Price,
        string Note
    );
}
