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
    public class ProductImageRepository : Repository<ProductImages>, IProductImageRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductImageRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;

        }

       

        public void Update(ProductImages obj)
        {
            _context.ProductImages.Update(obj);
        }
    }
}
