using BookStore_RazorTemp.Data;
using BookStore_RazorTemp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookStore_RazorTemp.Pages.Categories
{
    [BindProperties]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public Category Category { get; set; }
        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }
        public void OnGet()
        {
           
        }
        public IActionResult OnPost()
        {
            _context.Add(Category);
            _context.SaveChanges();
            return RedirectToPage("Index");
        }
    }
}
