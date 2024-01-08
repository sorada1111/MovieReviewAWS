using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace _301270677_301289381_Prathan_VinicioJacome__Lab3.Models
{
    public class MovieViewModel
    {
       
        public string MovieID { get; set; }
        [Required]
        public string Title { get; set; }

        [Required]
        public string Genre { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public List<string> Directors { get; set; } = new List<string>();

        [Required]
        public List<string> Actors { get; set; } = new List<string>();

        [Required]
        public IFormFile MovieFile { get; set; }

        [Required]
        public IFormFile ImageFile { get; set; }

        public String MovieFileUrl { get; set; }
        public String ImageFileUrl { get; set; }

        public string UserID { get; set; }
    }
}
