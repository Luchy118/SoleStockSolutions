using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoleStockSolutions.Controllers;
using SoleStockSolutions.Models;
using System.Web.Mvc;
using Moq;
using System.Linq;
using System.Collections.Generic;
using System.Web.Routing;
using System.Web;

namespace SoleStockSolutions.Tests.Controllers
{
    [TestClass]
    public class AccountControllerTests
    {
        private Mock<TFCEntities> mockDb;
        private AccountController controller;

        [TestInitialize]
        public void Setup()
        {
            mockDb = new Mock<TFCEntities>();
            controller = new AccountController();
        }

        private void SetUpControllerContext(AccountController controller)
        {
            var httpContext = new Mock<HttpContextBase>();
            var session = new Mock<HttpSessionStateBase>();
            httpContext.Setup(ctx => ctx.Session).Returns(session.Object);
            var requestContext = new RequestContext(httpContext.Object, new RouteData());
            controller.ControllerContext = new ControllerContext(requestContext, controller);
        }

        [TestMethod]
        public void ValidateLogin_UserExistsAndPasswordCorrect_ReturnsSuccess()
        {
            // Arrange
            var user = new Usuarios { email = "test@g.c", contrasenia = controller.HashPassword("Hola123!") };
            mockDb.Setup(db => db.Usuarios).Returns(new List<Usuarios> { user }.AsQueryable().BuildMockDbSet().Object);
            SetUpControllerContext(controller);

            // Act
            var result = controller.ValidateLogin(new Usuarios { email = "test@g.c", contrasenia = "Hola123!" }) as JsonResult;
            dynamic data = result.Data;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(data.success);
        }

        [TestMethod]
        public void ValidateLogin_UserExistsAndPasswordIncorrect_ReturnsFailure()
        {
            // Arrange
            var user = new Usuarios { email = "test@example.com", contrasenia = controller.HashPassword("password") };
            mockDb.Setup(db => db.Usuarios).Returns(new List<Usuarios> { user }.AsQueryable().BuildMockDbSet().Object);
            SetUpControllerContext(controller);

            // Act
            var result = controller.ValidateLogin(new Usuarios { email = "test@g.c", contrasenia = "wrongpassword" }) as JsonResult;
            dynamic data = result.Data;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(data.success);
            Assert.AreEqual("La contraseña es errónea.", data.message);
        }

        [TestMethod]
        public void ValidateLogin_UserDoesNotExist_ReturnsFailure()
        {
            // Arrange
            mockDb.Setup(db => db.Usuarios).Returns(new List<Usuarios>().AsQueryable().BuildMockDbSet().Object);
            SetUpControllerContext(controller);

            // Act
            var result = controller.ValidateLogin(new Usuarios { email = "nonexistent@example.com", contrasenia = "password" }) as JsonResult;
            dynamic data = result.Data;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(data.success);
            Assert.AreEqual("No existe ninguna cuenta registrada con ese nombre de usuario o correo electrónico.", data.message);
        }
    }
}
