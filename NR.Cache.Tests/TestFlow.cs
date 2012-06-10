using NUnit.Framework;

namespace NR.Cache.Tests
{
    [TestFixture]
    public abstract class TestFlow
    {
        protected abstract void Arrange();

        protected abstract void Act();
        
        [SetUp]
        public void SetUp()
        {
            Arrange();
            Act();
        }
    }
}