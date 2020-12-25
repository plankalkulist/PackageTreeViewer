using System;

namespace ACME.PRJ.PackageTreeViewer.Model
{
	/// <summary>
	/// Фабрика узлов дерева (просто для удобства)
	/// </summary>
	public class NodeFactory
	{
		/// <summary>
		/// Базовая инициализация нового узла
		/// </summary>
		internal Action<Node> _BaseInitialization { get; }

		/// <summary>
		/// .ctor
		/// </summary>
		public NodeFactory(Action<Node> baseInitialization)
		{
			_BaseInitialization = baseInitialization ?? throw new ArgumentNullException(nameof(baseInitialization));
		}

		/// <summary>
		/// Создание нового узла
		/// </summary>
		public Node CreateNode(object data, Action<Node> initializationAction = null)
		{
			var instance = new Node
			{
				Data = data
			};
			_BaseInitialization(instance);
			initializationAction?.Invoke(instance);
			return instance;
		}

		/// <summary>
		/// Создание нового узла с ошибкой
		/// </summary>
		public Node CreateNode(object data, object error, Action<Node> initializationAction = null)
		{
			var instance = new Node
			{
				Data = data,
				Error = error
			};
			_BaseInitialization(instance);
			initializationAction?.Invoke(instance);
			return instance;
		}
	}

	/// <summary>
	/// Расширения для фабрики узлов
	/// </summary>
	public static class NodeFactoryExtensions
	{
		/// <summary>
		/// Создание нестандартного узла
		/// </summary>
		public static TNode CreateNode<TNode>(this NodeFactory nodeFactory, object data, Action<TNode> initializationAction = null)
			where TNode: Node, new()
		{
			var instance = new TNode();
			instance.Data = data;
			nodeFactory._BaseInitialization(instance);
			initializationAction?.Invoke(instance);
			return instance;
		}
	}
}
