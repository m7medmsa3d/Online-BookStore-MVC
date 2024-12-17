using Book_Store.Data;
using BookStore.DataAccess.Repository.IRepository;
using BookStore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;

        }
     
      


        public void Update(Product obj)
        {
            Product product = _context.Products.FirstOrDefault(u=> u.Id == obj.Id);
            if (product != null)
            {
                product.Title = obj.Title;
                product.Description = obj.Description;
                product.CategoryId = obj.CategoryId;
                product.ISBN = obj.ISBN;
                product.Price = obj.Price;
                product.ListPrice = obj.ListPrice;
                product.Price100 = obj.Price100;
                product.Price50 = obj.Price50;
                product.ProductImages = obj.ProductImages;
              
                
            }
        }
    }
}
