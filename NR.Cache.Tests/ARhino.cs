using Rhino.Mocks;

namespace NR.Cache.Tests
{
    public static class A
    {
        public static T Mock<T>() where T : class
        {
            return MockRepository.GenerateMock<T>();
        }

        public static T Stub<T>() where T : class
        {
            return MockRepository.GenerateStub<T>();
        }
    }
}