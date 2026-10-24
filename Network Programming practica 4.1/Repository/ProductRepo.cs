using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Network_Programming_practica_4._1.DataAccess;
using Network_Programming_practica_4._1.Entities;

namespace Network_Programming_practica_4._1.Repository
{
    public class ProductRepo
    {
        private Mutex _mutex=new Mutex(false,"GlobalMutex");
        private readonly FakeApiDBContext _contex;
        public ProductRepo()
        {
            _contex = new FakeApiDBContext();
        }

        public async Task<ICollection<Product>> GetProducts()
        {
            return await _contex.products.ToArrayAsync(); ;
        }
        public async Task<Product?> GetProductById(int id)
        {
            return await _contex.products.FirstOrDefaultAsync(x=>x.ID==id);
        }
        public async Task<bool> AddProduct(Product product)
        {
            await _contex.products.AddAsync(product);
            return _contex.SaveChanges()>0;
        }
        public bool UpdateProduct(Product product)//Critical Section
        {
            try
            {
                _mutex.WaitOne();
                _contex.products.Update(product);
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
            
            return _contex.SaveChanges() > 0;
        }
        public bool RemoveProduct(Product product)//Critical Section
        {
            try
            {
                _mutex.WaitOne();
                _contex.products.Remove(product);
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
            
            return _contex.SaveChanges() > 0;
        }
    }
}
