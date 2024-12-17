using BookStore_RazorTemp.Data;
using BookStore_RazorTemp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookStore_RazorTemp.Pages.Categories
{
    [BindProperties]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public Category Category { get; set; }
        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }
        public void OnGet(int? id)
        {
            if (id != 0 && id != null)
            {
                Category = _context.Categories.Find(id);
            }

        }
        public IActionResult OnPost() 
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Update(Category);
                _context.SaveChanges();
                //TempData["Success"] = "Category Updated successfully";
                return RedirectToPage(nameof(Index));
            }
           
            return Page();
        }
    }
}
