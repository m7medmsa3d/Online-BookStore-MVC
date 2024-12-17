using Book_Store.Data;
using BookStore.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookStore.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        public ICategoryRepository Category { get; private set; }
        public IProductRepository Product { get; private set; }

        public ICompanyRepository Company { get; private set; }
        public IShoppingCartRepository ShoppingCart { get; private set; }
        public IApplicationUserRepository  ApplicationUser { get; private set; }
		public IOrderHeaderRepository OrderHeader { get; private set; }
		public IOrderDetailRepository OrderDetail { get; private set; }
        public IProductImageRepository ProductImage { get; private set; }
        public UnitOfWork(ApplicationDbContext context) 
        {
            _context = context;
            Category = new CategoryRepository(context);
            Product = new ProductRepository(context);
            Company = new CompanyRepository(context);
            ShoppingCart = new ShoppingCartRepository(context);
            ApplicationUser = new ApplicationUserRepository(context);
            OrderHeader = new OrderHeaderRepository(context);
            OrderDetail = new OrderDetailRepository(context);
            ProductImage = new ProductImageRepository(context);

        }

       

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
