using Moq;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

public static class DbSetExtensions
{
    public static Mock<DbSet<T>> BuildMockDbSet<T>(this IQueryable<T> source) where T : class
    {
        var mockDbSet = new Mock<DbSet<T>>();
        mockDbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(source.Provider);
        mockDbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(source.Expression);
        mockDbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(source.ElementType);
        mockDbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(source.GetEnumerator());
        return mockDbSet;
    }
}
