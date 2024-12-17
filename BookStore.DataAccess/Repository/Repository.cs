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
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        internal DbSet<T> dbset ;
        public Repository(ApplicationDbContext context)
        {
            _context = context;
            dbset = _context.Set<T>();
            _context.Products.Include(u => u.Category).Include(u => u.CategoryId);
        }
        public void Add(T entity)
        {
          this.dbset.Add(entity);
        }

        public T Get(Expression<Func<T, bool>>? filter, string? includeproperties = null,bool Tracked = false)
        {
            IQueryable<T> query ;
            if (Tracked)
            {
                query = dbset;
            }
            else
            {
                query = dbset.AsNoTracking();
            }
            query = query.Where(filter);
            if (includeproperties != null)
            {
                foreach (var includeprop in includeproperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeprop);
                }
            }
            return query.FirstOrDefault();
        }

        public IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter = null , string? includeproperties = null)
        {
            IQueryable<T> query = dbset;
            if (filter != null)
            {
                query = query.Where(filter);
            }


            if (includeproperties != null)
            {
                foreach (var includeprop in includeproperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeprop);
                }
            }
            return query.ToList();  
        }

        public void Remove(T entity)
        {
            dbset.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            dbset.RemoveRange(entities);
        }
    }
}
