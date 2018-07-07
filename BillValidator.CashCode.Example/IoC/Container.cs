using System;
using System.Collections.Generic;
using Unity;
using Unity.Injection;
using Unity.Lifetime;
using Unity.Resolution;

namespace BillValidator.CashCode.Example.IoC
{
    public sealed class Container
    {
        /// <summary>
        /// The instance of the IoC Container.
        /// </summary>
        private static readonly Container instance = new Container();

        private readonly IUnityContainer _iocContainer = new UnityContainer();

        public IUnityContainer IocContainer => _iocContainer;

        /// <summary>
        /// Prevents the creation of an instance of the Container type.
        /// </summary>
        private Container()
        {
        }

        /// <summary>
        /// Initializes static members of the Container type.
        /// </summary>
        static Container()
        {
            Instance._iocContainer.RegisterInstance(Instance, new ContainerControlledLifetimeManager());

        }

        /// <summary>
        /// Gets the singleton instance of the Container class.
        /// </summary>
        public static Container Instance => instance;

        /// <summary>
        /// Registers the TBase type to the TImplementation type.
        /// </summary>
        /// <typeparam name="TBase">The base type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        public void Register<TBase, TImplementation>() where TImplementation : TBase
        {
            _iocContainer.RegisterType(typeof(TBase), typeof(TImplementation), new TransientLifetimeManager());
        }

        /// <summary>
        /// Registers the TBase type to a singleton of the provided value.
        /// </summary>
        /// <typeparam name="TBase">The base type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <param name="value">The singleton value.</param>
        public void Register<TBase, TImplementation>(TImplementation value) where TImplementation : TBase
        {
            _iocContainer.RegisterInstance(typeof(TBase), value, new ContainerControlledLifetimeManager());
        }

        /// <summary>
        /// Resolves the provided type.
        /// </summary>
        /// <typeparam name="T">Resolves the type T.</typeparam>
        /// <returns>The resolved value.</returns>
        public T Resolve<T>()
        {
            return (T)_iocContainer.Resolve(typeof(T));
        }

        /// <summary>
        /// Resolves the type T using the provided parameters.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public T Resolve<T>(Dictionary<string, object> parameters)
        {
            var parameterOverrides = new ParameterOverrides();
            foreach (var parameter in parameters)
            {
                parameterOverrides.Add(parameter.Key, parameter.Value);
            }

            return (T)_iocContainer.Resolve(typeof(T), parameterOverrides.OnType<T>());
        }

        /// <summary>
        /// Injects in constructor TUsage the type TBase
        /// </summary>
        /// <typeparam name="TUsage"></typeparam>
        /// <typeparam name="TBase"></typeparam>
        public void RegisterInConstructor<TUsage, TBase>()
        {
            _iocContainer.RegisterType<TUsage>(new InjectionConstructor(Instance.Resolve<TBase>()));
        }

        /// <summary>
        /// Register a class with an interface which are Open Generics
        /// </summary>
        /// <param name="basic">Open Generic Interface</param>
        /// <param name="implementation">Open Generic Implementation</param>
        public void Register(Type basic, Type implementation)
        {
            _iocContainer.RegisterType(basic, implementation, new TransientLifetimeManager());
        }
    }
}
