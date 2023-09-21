using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Operations;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitTest.Web.Controllers;
using UnitTest.Web.Models;
using UnitTest.Web.Repository;

namespace UnitTest.Test
{
    public class ProductControllerTest
    {
        private readonly Mock<IRepository<Product>> _mockRepo;

        private readonly ProductsController _controller;

        private List<Product> products;

        public ProductControllerTest()
        {
            _mockRepo = new Mock<IRepository<Product>>();
            _controller = new ProductsController(_mockRepo.Object);

            products = new List<Product>()
            {
                new Product{Id=1, Name="Kalem", Price=100, Stock=50, Color="Kırmızı"},
                new Product{Id=2, Name="Defter", Price=200, Stock=500, Color="Mavi"}
            };
        }


        [Fact] // Burada metodun bir test olduğunu belirliyoruz.
        public async void Index_ActionExecutes_ReturnView() 
        {
            var result = await _controller.Index();

            Assert.IsType<ViewResult>(result);
            // Testin başarılı olup olmadığının kontrolü burada yapılıyor.
            // Breakpoint ile de kontrol etmek istersek testi debug modunda çalıştırabiliriz.
        }

        [Fact]
        public async void Index_ActionExecutes_ReturnProductList()
        {
            _mockRepo.Setup(repo => repo.GetAll()).ReturnsAsync(products); 
            // Burada soyut bir product listesi, sanki Repository'nin dönüş değeriymiş gibi belirliyoruz
            // GetAll() çağrılmasının sonucunu Constructor'da oluşturduğumuz liste olarak ayarlıyoruz aslında

            var result = await _controller.Index();
            var viewResult = Assert.IsType<ViewResult>(result);

            var productList = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.Model);

            Assert.Equal<int>(2,productList.Count());
        }

        [Fact]
        public async void Details_IdIsNull_ReturnRedirectToAction()
        {
            var result = await _controller.Details(null);
            // Sanki null değer gelmiş gibi hayal edebiliriz.

            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async void Details_IdInValid_ReturnNotFound() 
        {
            Product product = null;
            _mockRepo.Setup(x=>x.GetById(0)).ReturnsAsync(product);
            // Burada direkt null yazsaydık, dönüş değeri Product olmadığı için hata alırdık.

            var result = await _controller.Details(0);
            
            var redirect = Assert.IsType<NotFoundResult>(result);

            Assert.Equal<int>(404, redirect.StatusCode); // Eğer Ok'u kabul etseydik 202, gibi gibi status koduna göre değerlendirme yapacaktık
        }

        [Theory] // Theory ile adı üzerinde bir varsayım oluşturuyoruz.
        [InlineData(1)] // Parametrizasyonu InlineData ile sağlıyoruz. Bu notasyonda yazdığımız veri parametreye eşleniyor.
        public async void Details_ValidId_ReturnProduct(int productId)
        {
            Product product = products.First(x => x.Id == productId);

            _mockRepo.Setup(repo => repo.GetById(productId)).ReturnsAsync(product);

            var result = await _controller.Details(productId);

            var viewResult = Assert.IsType<ViewResult>(result);

            var resultProduct = Assert.IsAssignableFrom<Product>(viewResult.Model);

            Assert.Equal(product.Id, resultProduct.Id);
            Assert.Equal(product.Name,resultProduct.Name);
        }

        [Fact]
        public void Create_ActionExecutes_ReturnView()
        {
            var result = _controller.Create();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async void CreatePost_InvalidModelState_ReturnView()
        {
            _controller.ModelState.AddModelError("Name", "Name alanı gereklidir.");

            var result = await _controller.Create(products.First());

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.IsType<Product>(viewResult.Model);
            // Validasyon hatası eklediğimiz için Controller tarafında if kısmı es geçilecektir.
            // Bu bağlamda RedirectToAction yerine View dönüş değeri çalışacaktır.
        }

        [Fact]
        public async void CreatePost_ValidModelState_ReturnRedirectToIndexAction()
        {
            var result = await _controller.Create(products.First());
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal<string>("Index", redirect.ActionName);
            // Bir önceki testin tam tersi şekilde, validasyon hatası almadığımız için RedirectToAction dönüş tipi çalıştı.
        }

        [Fact]
        public async void CreatePost_ValidModelState_CreateMethodExecute()
        {
            Product newProduct = null;

            _mockRepo.Setup(x => x.Create(It.IsAny<Product>())).Callback<Product>(y => newProduct = y);
            // Callback kullanımı ile başlangıçta null olan newProduct değişkenini, Create kullanıldıktan sonra üretilen nesneye atadık.

            var result = await _controller.Create(products.First());

            _mockRepo.Verify(repo => repo.Create(It.IsAny<Product>()), Times.Once);

            Assert.Equal(products.First().Id, newProduct.Id);
        }

        [Fact]
        public async void CreatePost_InvalidModelState_NeverCreateExecute()
        {
            _controller.ModelState.AddModelError("Name", "Name alanı gereklidir.");

            var result = await _controller.Create(products.First());

            _mockRepo.Verify(x => x.Create(It.IsAny<Product>()), Times.Never);

            // Burada parantez içerisindeki işlemin hiç yapılmadığını kontrol ettik.

        }

        [Fact]
        public async void Edit_IdIsNull_RedirectToIndexAction()
        {
            var result = await _controller.Edit(null);

            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Index", redirect.ActionName);

        }

        [Theory]
        [InlineData(3)]
        public async void Edit_IdInValid_ReturnNotFound(int productId)
        {
            Product product = null;

            _mockRepo.Setup(x => x.GetById(productId)).ReturnsAsync(product);

            var result = await _controller.Edit(productId);

            var redirect = Assert.IsType<NotFoundResult>(result);

            Assert.Equal<int>(404, redirect.StatusCode);

        }

        [Theory]
        [InlineData(2)]
        public async void Edit_ActionExecute_ReturnProduct(int productId)
        {
            var product = products.First(x => x.Id == productId);

            _mockRepo.Setup(repo => repo.GetById(productId)).ReturnsAsync(product);

            var result = await _controller.Edit(productId);

            var viewResult = Assert.IsType<ViewResult>(result);

            var resultProduct = Assert.IsAssignableFrom<Product>(viewResult.Model);

            Assert.Equal(product.Id, resultProduct.Id);
            Assert.Equal(product.Name, resultProduct.Name);
        }

        [Theory]
        [InlineData(1)]
        public void EditPOST_IdIsNotEqualProduct_ReturnNotFound(int productId)
        {
            var result = _controller.Edit(2, products.First(x => x.Id == productId));

            var redirect = Assert.IsType<NotFoundResult>(result);
        }

        [Theory]
        [InlineData(1)]
        public void EditPOST_InValidModelState_ReturnView(int productId)
        {
            _controller.ModelState.AddModelError("Name", "");

            var result = _controller.Edit(productId, products.First(x => x.Id == productId));

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.IsType<Product>(viewResult.Model);
        }

        [Theory]
        [InlineData(1)]
        public void EditPOST_ValidModelState_ReturnRedirectToIndexAction(int productId)
        {
            var result = _controller.Edit(productId, products.First(x => x.Id == productId));

            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Index", redirect.ActionName);
        }

        [Theory]
        [InlineData(1)]
        public void EditPOST_ValidModelState_UpdateMethodExecute(int productId)
        {
            var product = products.First(x => x.Id == productId);

            _mockRepo.Setup(repo => repo.Update(product));


            _controller.Edit(productId, product);

            _mockRepo.Verify(repo => repo.Update(It.IsAny<Product>()), Times.Once);
        }

        [Fact]
        public async void Delete_IdIsNull_ReturnNotFound()
        {
            var result = await _controller.Delete(null);

            Assert.IsType<NotFoundResult>(result);
        }

        [Theory]
        [InlineData(0)]
        public async void Delete_IdIsNotEqualProduct_ReturnNotFound(int productId)
        {
            Product product = null;

            _mockRepo.Setup(x => x.GetById(productId)).ReturnsAsync(product);

            var result = await _controller.Delete(productId);

            Assert.IsType<NotFoundResult>(result);
        }

        [Theory]
        [InlineData(1)]
        public async void Delete_ActionExecutes_ReturnProduct(int productId)
        {
            var product = products.First(x => x.Id == productId);

            _mockRepo.Setup(repo => repo.GetById(productId)).ReturnsAsync(product);
            // Setup ile esasında hedef Controller'da çağrılan Repository metotlarının dönüş değerleri ile ilgileniyoruz.

            var result = await _controller.Delete(productId);

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.IsAssignableFrom<Product>(viewResult.Model);
        }

        [Theory]
        [InlineData(1)]
        public async void DeleteConfirmed_ActionExecutes_ReturnRedirectToIndexAction(int productId)
        {
            var result = await _controller.DeleteConfirmed(productId);

            Assert.IsType<RedirectToActionResult>(result);
        }

        [Theory]
        [InlineData(1)]
        public async void DeleteConfirmed_ActionExecutes_DeleteMethodExecute(int productId)
        {
            var product = products.First(x => x.Id == productId);

            _mockRepo.Setup(repo => repo.Delete(product));

            await _controller.DeleteConfirmed(productId);

            _mockRepo.Verify(repo => repo.Delete(It.IsAny<Product>()), Times.Once);
            // Delete metodunun çalışıp çalışmadığını kontrol ediyoruz.
        }
    }
}
