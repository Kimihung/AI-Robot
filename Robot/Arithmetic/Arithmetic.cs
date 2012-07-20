/*************************************************************************
 *
 * Copyright (c) 2009-2012 Xuld. All rights reserved.
 * 
 * Project Url: http://work.xuld.net/coreplus
 * 
 * This source code is part of the Project CorePlus for .Net.
 * 
 * This code is licensed under CorePlus License.
 * See the file License.html for the license details.
 * 
 * 
 * You must not remove this notice, or any other, from this software.
 *
 * 
 *************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using CorePlus.IO;

namespace CorePlus.RunTime {

	/// <summary>
	/// �ṩ��ѧ���ʽ��ʵʱ���㹦�ܡ�
	/// </summary>
	/// <remarks>
	/// Ҫ����ʽ�Կո���Ϊ�ָ���
	/// ת�����ʽ�۷�Ϊ��
	/// ��������ֵ ,����������Ϊ@
	/// �ַ�������
	/// �������{+��-��*��/��++��+=��--��-=��*=��/=��!��!=��&gt;��&gt;=��&gt;&gt;��&lt;��&lt;=��&lt;&gt;��|��|=��||��&amp;��&amp;=��&amp;&amp;}
	/// ����{����(��)}
    /// 
    /// <example>
    /// ����ʾ����ʾ�����ʹ�� <see cref="Arithmetic"/> ������ʽ��
    /// <code>
    /// using System;
    /// using CorePlus.RunTime;
    /// 
    /// class Sample{
    /// 
    ///     public static void Main(){
    ///         Arithmetic calc = new Arithmetic();
    ///         double result = (double)calc.Compute("1 + 2sin 4");
    ///         Console.Write(result);
    ///         Console.Read();
    ///     }
    /// }
    /// </code>
    /// һ�����ʽ�еĺ������Զ��塣
    /// ������ʾ������Զ��庯����
    /// <code>
    /// using System;
    /// using CorePlus.RunTime;
    /// 
    /// class Sample{
    /// 
    ///     public static void Main(){
    ///         Arithmetic calc = new Arithmetic();
    ///         calc.Operators.Add(new Arithmetic.FunctionCallOperator("in", v => v + 1);
    ///         double result = (double)calc.Compute("1 + in 4");
    ///         Console.Write(result); //  1 + ( 4 + 1 ) == 6
    ///         Console.Read();
    ///     }
    /// }
    /// </code>
    /// </example>
    /// Ĭ�ϼ�����ֱ��֧�� Boolean Int32 Double Char String �� 4 �����͡�������б��ʽ�Ϸ�:
    /// 1 + ( 2 == 2 )   // 2
    /// "1" + 'd'  //  "1d"
    /// true || false // true
	/// </remarks>
	public sealed class Arithmetic {

		#region ��

		/// <summary>
		/// ���ʽ�ָ�Ϊ�б���ʽ��
		/// </summary>
		/// <param name="expression">���ʽ��</param>
		/// <returns>���ʽ��</returns>
		/// <exception cref="ArgumentNullException"><paramref name="expression" /> Ϊ�ա�</exception>
        /// <exception cref="ArithmeticException"><paramref name="expression" /> ���ǺϷ��ı��ʽ��</exception>
		List<Token> ParseExpression(string expression) {
			List<Token> list = new List<Token>();
			int count = expression.Length - 1;
			char c;
			string opt = null;
			bool? flag = null;
			TokenType lastTokenType = TokenType.LeftBucket;
			for (int i = 0; i <= count; i++) {
				c = expression[i];

				#region ����

				if ((c >= '0' && c <= '9') || c == '.') {
					opt = Scanner.ReadNumber(expression, ref i);
					list.Add(new Token {
						Value = opt,
						Type = lastTokenType = opt.IndexOf('.') == -1 ? TokenType.Integer : TokenType.Double
					});
					continue;
				}

				#endregion

				#region ����

				if (c == '$') {
					if (lastTokenType == TokenType.Integer || lastTokenType == TokenType.Double) {
						list.Add(new Token {
							Value = "*",
							Type = TokenType.Operator
						});
					}
					i++;
					list.Add(new Token {
						Value = Scanner.ReadValue(expression, ref i, rrr => char.IsLetter(rrr) || rrr == '.'),
						Type = lastTokenType = TokenType.Variant
					});
					continue;
				}
				
				#endregion

				#region �ַ�

				if (char.IsLetter(c)) {
					if (lastTokenType == TokenType.Integer || lastTokenType == TokenType.Double) {
						list.Add(new Token {
							Value = "*",
							Type = TokenType.Operator
						});
					}
					opt = Scanner.ReadValue(expression, ref i, char.IsLetter);
					if (opt == "null")
						list.Add(new Token {
							Value = String.Empty,
							Type = lastTokenType = TokenType.String
						});
					else
						list.Add(new Token {
							Value = opt,
							Type = lastTokenType = opt == "true" || opt == "false" ? TokenType.Boolean : TokenType.Operator
						});
					continue;
				}

				#endregion

				#region �ַ��б�
				switch (c) {
					#region ( )
					case '(':
						list.Add(new Token {
							Type = lastTokenType = TokenType.LeftBucket
						});
						continue;
					case ')':
						list.Add(new Token {
							Type = lastTokenType = TokenType.RightBucket
						});
						continue;
					case ' ':
						continue;
					#endregion

					#region + - * / %
					case '+':
					case '-':
						if (lastTokenType == TokenType.Operator) {
							FillBucket(list, ref flag);
						}
						if (i >= count) {
							opt = c.ToString();
						} else {
							char c1 = expression[++i];
							if (c1 == c || c1 == '=') {
								opt = new String(new char[] { c, c1 });
							} else {
								opt = c.ToString();
								if (c1 != ' ')
									i--;
							}
						}


						break;
					case '*':
					case '/':
					case '%':
						if (i >= count) {
							opt = c.ToString();
						} else {
							if (expression[++i] == '=') {
								opt = new String(new char[] { c, '=' });
							} else {
								opt = c.ToString();
								if (expression[i] != ' ')
									i--;
							}
						}
						break;
					#endregion

					#region �ַ���

					case '\"':
					case '\'':
						Token t = new Token();
						t.Type = lastTokenType = c == '\"' ? TokenType.String : TokenType.Char;
						t.Value = Scanner.ReadString(expression, ref i);
						if (t.Value == null)
							throw new SyntaxException("\"δ�ر�");
						list.Add(t);
						continue;

					#endregion

					#region > < =
					case '>':
						if (i >= count) {
							opt = ">";
						} else
							switch (c = expression[++i]) {
								case '>':
								case '=':
									opt = ">" + c;
									break;
								case ' ':
									opt = ">";
									break;
								default:
									opt = ">";
									i--;
									break;
							}
						break;
					case '<':
						if (i >= count) {
							opt = "<";
						} else
							switch (c = expression[++i]) {
								case '<':
								case '=':
									opt = "<" + c;
									break;
								case '>':
									opt = "!=";
									break;
								case ' ':
									opt = "<";
									break;
								default:
									opt = "<";
									i--;
									break;
							}
						break;
					case '~':
						if (i >= count) {
							opt = "~";
						} else
							switch (expression[++i]) {
								case '=':
									opt = "~=";
									break;
								default:
									opt = "~";
									i--;
									break;
							}
						break;
					case '=':
						if (i >= count) {
							opt = "==";
						} else
							switch (c = expression[++i]) {
								case '=':
								case ' ':
									opt = "==";
									break;
								case '>':
									opt = "=>";
									break;
								default:
									opt = "==";
									i--;
									break;
							}
						break;
					#endregion

					#region ! | &
					case '!':
						if (i >= count) {
							opt = "!";
						} else
							switch (c = expression[++i]) {
								case '=':
									opt = "!=";
									break;
								default:
									opt = "!";
									i--;
									break;
							}
						break;
					case '|':
						if (i >= count) {
							opt = "|";
						} else {
							switch (c = expression[++i]) {
								case '=':
									opt = "!=";
									break;
								case '|':
									opt = "||";
									break;
								case ' ':
									opt = "|";
									break;
								default:
									opt = "|";
									i--;
									break;
							}
						}
						break;
					case '&':
						if (i >= count) {
							opt = "&";
						} else {
							switch (c = expression[++i]) {
								case ' ':
									opt = "&";
									break;
								case '=':
									opt = "&=";
									break;
								case '&':
									opt = "&&";
									break;
								default:
									opt = "&";
									i--;
									break;
							}
						}
						break;
					#endregion

					#region ����

					case '\\':
						opt = "\\";
						break;
					case '^':
						opt = "^";
						break;
					#endregion

					default:
						throw new SyntaxException("���ܴ����ַ� " + c, SyntaxErrorType.Unexpected);
				}

				if (flag == true) {
					list.Add(new Token {
						Type = TokenType.RightBucket
					});
					flag = null;
				} else if (flag == false)
					flag = true;

				list.Add(new Token {
					Value = opt,
					Type = lastTokenType = TokenType.Operator
				});

				#endregion
			}

			if (flag != null) {
				list.Add(new Token {
					Type = TokenType.RightBucket
				});
			}

			return list;
		}

		static void FillBucket(List<Token> list, ref bool? flag) {
			if (flag != null) {
				list.Add(new Token {
					Type = TokenType.RightBucket
				});
			}
			list.Add(new Token {
				Type = TokenType.LeftBucket
			});


			flag = false;

		}

		/// <summary>
		/// ��׺���ʽת��Ϊ��׺���ʽ��
		/// </summary>
		/// <param name="ex">���ʽ��</param>
		/// <returns>���ʽ��</returns>
		List<Token> ConvertToPostfix(List<Token> ex) {

			int s = 0;
			Stack<Token> sOperator = new Stack<Token>();
			Token t;
			int count = ex.Count;
			int i = 0;
			while (i < count) {
				t = ex[i++];

				//������������ʱ���ǽ���ѹ��ջ��
				if (t.Type == TokenType.LeftBucket) {
					//  ex[s++] = t;
					sOperator.Push(t);
				} else if (t.Type == TokenType.RightBucket) {
					if (sOperator.Count == 0)
						throw new SyntaxException("����� )", SyntaxErrorType.Unexpected);
					while (sOperator.Count > 0) {
						if (sOperator.Peek().Type == TokenType.LeftBucket) {
							sOperator.Pop();
							break;
						}

						ex[s++] = sOperator.Pop();
					}
				} else if (t.IsVar) {
					ex[s++] = t;
				} else {

					//�������������tʱ��
					//����������a.��ջ���������ȼ����ڻ����t��������������͵���������У� 
					//����������b.t��ջ
					Operator tt, p = GetOperators(t.Value);

					while (sOperator.Count > 0 && sOperator.Peek().Type != TokenType.LeftBucket && p.Priority >= (tt = GetOperators(sOperator.Peek().Value)).Priority) {
						if (tt.IsSingle && p.IsSingle)
							break;
						ex[s++] = sOperator.Pop();
					}
					sOperator.Push(t);
				}

			}

			//��׺���ʽȫ���������ջ������������������͵����������
			while (sOperator.Count > 0) {
				ex[s++] = sOperator.Pop();
			}

			while (count-- > s)
				ex.RemoveAt(count);
			return ex;
		}

		/// <summary>
		/// �����׺���ʽ��
		/// </summary>
		/// <param name="expression">���ʽ��</param>
		/// <returns>ֵ��</returns>
		/// <exception cref="SyntaxException">���ʽ�������������</exception>
		object ComputePostfix(List<Token> expression) {
			//������һ��ջS
			Stack s = new Stack();
			foreach (Token t in expression) {
				switch (t.Type) {
					case TokenType.Integer:
						s.Push(int.Parse(t.Value, System.Globalization.CultureInfo.CurrentCulture));
						break;
					case TokenType.Operator:
						Operator opt = GetOperators(t.Value);
						if (s.Count == 0)
							throw new SyntaxException("ȱ�ٲ�������");
						if (opt.IsSingle) {
							s.Push(opt.Compute(s.Pop()));
						} else {
							object v = s.Pop();
							if (s.Count > 0) {
								if (s.Peek() != null)
									s.Push(opt.Compute(s.Pop(), v));
								else {
									s.Pop();
									s.Push(opt.Compute(v));
								}
							} else
								s.Push(opt.Compute(v));
						}
						break;
					case TokenType.Char:
						s.Push((int)char.Parse(t.Value));
						break;
					case TokenType.Double:
						s.Push(double.Parse(t.Value, System.Globalization.CultureInfo.CurrentCulture));
						break;
					case TokenType.LeftBucket:
						s.Push(null);
						break;
					case TokenType.String:
						s.Push(t.Value);
						break;
					case TokenType.Boolean:
						s.Push(bool.Parse(t.Value));
						break;
					case TokenType.Variant:
						s.Push(_variants[t.Value]);
						break;
				}


			}



			return s.Pop();

		}

		/// <summary>
		/// ������ʽ��
		/// </summary>
		/// <param name="expression">���ʽ��</param>
		/// <returns>����Ľ����</returns>
		/// <exception cref="SyntaxException">���ʽ�������������</exception>
		/// <exception cref="ArithmeticException">���ʽ�������������</exception>
		/// <exception cref="ArgumentNullException">���ʽ�ǿ��ַ�����</exception>
		/// <exception cref="DivideByZeroException">�� 0 ����</exception>
        public object Compute(string expression) {
			return ComputePostfix(ConvertToPostfix(ParseExpression(expression)));
		}

		/// <summary>
		/// ��ʼ�� <see cref="CorePlus.RunTime.Arithmetic"/> ����ʵ����
		/// </summary>
		public Arithmetic() {
			Setup();
		}

		#endregion

		#region ����

		/// <summary>
		/// �������ϡ�
		/// </summary>
		[Serializable]
		public class VariantCollection : System.Collections.Specialized.NameObjectCollectionBase, ICollection {

			/// <summary>
			/// ��ȡ������ָ�����ֵı���ֵ��
			/// </summary>
			/// <param name="varName">�������������� $ ��</param>
			/// <returns>ָ��������ֵ��</returns>
			/// <exception cref="ArithmeticException">���������ڡ�</exception>
			public object this[string varName] {
				get {
					object v = BaseGet(varName);
					if (v == null)
						throw new KeyNotFoundException("����" + varName + "δ���塣");
					return v;
				}
				set {
					BaseSet(varName, value);
				}
			}

			/// <summary>
			/// ���ض��� <see cref="T:System.Array"/> ��������ʼ���� <see cref="T:System.Collections.ICollection"/> ��Ԫ�ظ��Ƶ�һ�� <see cref="T:System.Array"/> �С�
			/// </summary>
			/// <param name="array">��Ϊ�� <see cref="T:System.Collections.ICollection"/> ���Ƶ�Ԫ�ص�Ŀ��λ�õ�һά <see cref="T:System.Array"/>��<see cref="T:System.Array"/> ������д��㿪ʼ��������</param>
			/// <param name="index"><paramref name="array"/> �д��㿪ʼ���������ڴ˴���ʼ���ơ�</param>
			/// <exception cref="T:System.ArgumentNullException">
			/// 	<paramref name="array"/> Ϊ null�� </exception>
			/// <exception cref="T:System.ArgumentOutOfRangeException">
			/// 	<paramref name="index"/> С���㡣 </exception>
			/// <exception cref="T:System.ArgumentException">
			/// 	<paramref name="array"/> �Ƕ�ά�ġ�- �� - <paramref name="index"/> ���ڻ���� <paramref name="array"/> �ĳ��ȡ�- �� - Դ <see cref="T:System.Collections.ICollection"/> �е�Ԫ����Ŀ���ڴ� <paramref name="index"/> ��Ŀ�� <paramref name="array"/> ĩβ֮��Ŀ��ÿռ䡣 </exception>
			/// <exception cref="T:System.ArgumentException">Դ <see cref="T:System.Collections.ICollection"/> �������޷��Զ�ת��ΪĿ�� <paramref name="array"/> �����͡� </exception>
			public void CopyTo(Array array, int index) {
				foreach (string vars in base.BaseGetAllValues())
					array.SetValue(vars, index++);
			}

		}

		/// <summary>
		/// ��ǰ�ı������ϡ�
		/// </summary>
		VariantCollection _variants = new VariantCollection();

		/// <summary>
		/// ��ȡ��ǰ�ı�����
		/// </summary>
		public VariantCollection Variants {
			get {
				return _variants;
			}
		}

		/// <summary>
		/// ��ʾ���ʽ��
		/// </summary>
		struct Token {

			/// <summary>
			/// ���ʽ�����͡�
			/// </summary>
			public TokenType Type;

			/// <summary>
			/// ���ʽ��ֵ��
			/// </summary>
			public string Value;

			/// <summary>
			/// ��ȡ��ǰ���ʽ�Ƿ�Ϊ������
			/// </summary>
			public bool IsVar {
				get {
					return Type != TokenType.Operator && Type != TokenType.LeftBucket && Type != TokenType.RightBucket;
				}
			}

			/// <summary>
			/// ���ظ�ʵ������ȫ�޶���������
			/// </summary>
			/// <returns>
			/// ������ȫ�޶��������� <see cref="T:System.String"/>��
			/// </returns>
			public override string ToString() {
				return Value;
			}
		}

		/// <summary>
		/// ��ʾһ�����ʽ�����͡�
		/// </summary>
		enum TokenType {

			/// <summary>
			/// ��������
			/// </summary>
			Operator,

			/// <summary>
			/// ������
			/// </summary>
			Variant,

			/// <summary>
			/// ������
			/// </summary>
			Integer,

			/// <summary>
			/// ������
			/// </summary>
			Boolean,

			/// <summary>
			/// ��������
			/// </summary>
			Double,

			/// <summary>
			/// �ַ�����
			/// </summary>
			String,

			/// <summary>
			/// �ַ���
			/// </summary>
			Char,

			/// <summary>
			/// �����š�
			/// </summary>
			LeftBucket,

			/// <summary>
			/// �����š�
			/// </summary>
			RightBucket

		}

		/// <summary>
		/// ��ʾһ����������
		/// </summary>
		public abstract class Operator:IComparable<Operator> {

			#region ����

			/// <summary>
			/// ��ȡ�����õ�ǰ�����������ȵȼ���
			/// </summary>
			public int Priority {
				get;
				set;
			}

			/// <summary>
			/// ��ȡ�����õ�ǰ�����������֡�
			/// </summary>
			public string Name {
				get;
				set;
			}

			/// <summary>
			/// ��ȡָʾ��ǰ�������Ƿ��ǵ�Ŀ�����Ĳ���ֵ��
			/// </summary>
			public abstract bool IsSingle {
				get;
			}


			#endregion

			#region ����

			/// <summary>
			/// ��ʼ�� <see cref="CorePlus.RunTime.Arithmetic.Operator"/> ����ʵ����
			/// </summary>
			/// <param name="name">���֡�</param>
			/// <param name="priority">���ȵȼ���</param>
			protected Operator(string name, int priority) {
				Name = name;
				Priority = priority;
			}

			/// <summary>
			/// ���ر�ʾ��ǰ <see cref="T:System.Object"/> �� <see cref="T:System.String"/>��
			/// </summary>
			/// <returns>
			/// 	<see cref="T:System.String"/>����ʾ��ǰ�� <see cref="T:System.Object"/>��
			/// </returns>
			public override string ToString() {
				return Name;
			}

			/// <summary>
			/// ͨ����ǰ������������ʽ��ֵ��
			/// </summary>
			/// <param name="right">Ҫ�����ֵ��</param>
			/// <returns>����Ľ����</returns>
			public abstract object Compute(object right);

			/// <summary>
			/// ͨ����ǰ������������ʽ��ֵ��
			/// </summary>
			/// <param name="left">��ֵ��</param>
			/// <param name="right">��ֵ��</param>
			/// <returns>ֵ��</returns>
			/// <exception cref="SyntaxException">�޷����㡣</exception>
			public abstract object Compute(object left, object right);

			/// <summary>
			/// �����㷢������ʱ���С�
			/// </summary>
			/// <param name="left">��ֵ��</param>
			/// <param name="right">��ֵ��</param>
			/// <returns>ֵ��</returns>
			protected object OnError(object left, object right) {
				throw new ArithmeticException(String.Format("�޷�������ʽ {{{0} {1} {2}}} ��ֵ��", left, Name, right));
			}

			/// <summary>
			/// �����㷢������ʱ���С�
			/// </summary>
			/// <param name="right">Ҫ�����ֵ��</param>
			/// <returns>ֵ��</returns>
			protected object OnError(object right) {
                throw new ArithmeticException(String.Format("�޷�������ʽ {{{0}{1}}} ��ֵ��", Name, right));
			}

			#endregion

            #region IComparable<Operator> ��Ա

            /// <summary>
            /// �Ƚϵ�ǰ�����ͬһ���͵���һ����
            /// </summary>
            /// <param name="other">��˶�����бȽϵĶ���</param>
            /// <returns>һ�� 32 λ�з���������ָʾҪ�ȽϵĶ�������˳�򡣷���ֵ�ĺ������£� ֵ ���� С���� �˶���С�� other ������ �� �˶������ other�� ������ �˶������ other��</returns>
            public int CompareTo(Operator other) {
                return Name.CompareTo(other);
            }

            #endregion
        }

		/// <summary>
		/// ��ʾ�������Ĳ�������
		/// </summary>
		public class ArithmeticOperator : Operator {

			/// <summary>
			/// ��ʼ�� <see cref="CorePlus.RunTime.Arithmetic.ArithmeticOperator"/> ����ʵ����
			/// </summary>
			/// <param name="name">���֡�</param>
			/// <param name="priority">���ȵȼ���</param>
			/// <param name="intC">�����ί�С�</param>
			/// <param name="doubleC">�����ί�С�</param>
            /// <exception cref="ArgumentNullException"><paramref name="intC" /> �� <paramref name="doubleC" /> Ϊ�ա�</exception>
			public ArithmeticOperator(string name, int priority, Func<int, int, int> intC, Func<double, double, double> doubleC)
				: base(name, priority) {
				_getter1 = intC;
				_getter2 = doubleC;
			}

			Func<int, int, int> _getter1;

			Func<double, double, double> _getter2;

			/// <summary>
			/// ͨ����ǰ������������ʽ��ֵ��
			/// </summary>
			/// <param name="right">Ҫ�����ֵ��</param>
			/// <returns>����Ľ����</returns>
			public override object Compute(object right) {

				switch (Name) {
					case "+":
						return right;
					case "-":
						if (right is int)
							return -(int)right;
						if (right is double)
							return -(double)right;
						if (right is bool)
							return (bool)right ? -1 : 0;
						break;

				}

				return OnError(right);

			}

			/// <summary>
			/// ��ȡָʾ��ǰ�������Ƿ��ǵ�Ŀ�����Ĳ���ֵ��
			/// </summary>
			/// <value></value>
			public override bool IsSingle {
				get {
					return false;
				}
			}

			/// <summary>
			/// ͨ����ǰ������������ʽ��ֵ��
			/// </summary>
			/// <param name="left">��ֵ��</param>
			/// <param name="right">��ֵ��</param>
			/// <returns>ֵ��</returns>
			/// <exception cref="SyntaxException">�޷����㡣</exception>
			public override object Compute(object left, object right) {
				if (left is bool)
					left = (bool)left ? 1 : 0;
				if (right is bool)
					right = (bool)right ? 1 : 0;
				if (left is int)
					if (right is int)
						return _getter1((int)left, (int)right);
					else
						left = (double)(int)left;
				if (left is double)
					if (right is double)
						return _getter2((double)left, (double)right);
					else if (right is int)
						return _getter2((double)left, (double)(int)right);

                if (Name == "+" && right != null) {
                    return String.Concat(left.ToString(), right.ToString());

                }

				return OnError(left, right);

			}

		}

		/// <summary>
		/// ��ʾתΪ�����Ĳ�������
		/// </summary>
		public class BoolOperator : Operator {

			/// <summary>
			/// ��ʼ�� <see cref="CorePlus.RunTime.Arithmetic.BoolOperator"/> ����ʵ����
			/// </summary>
			/// <param name="name">���֡�</param>
			/// <param name="priority">���ȵȼ���</param>
			public BoolOperator(string name, int priority)
				: base(name, priority) {
			}

			/// <summary>
			/// ��ȡָʾ��ǰ�������Ƿ��ǵ�Ŀ�����Ĳ���ֵ��
			/// </summary>
			/// <value></value>
			public override bool IsSingle {
				get {
					return Name == "!";
				}
			}

			#region ��Ŀ����


			/// <summary>
			/// ͨ����ǰ������������ʽ��ֵ��
			/// </summary>
			/// <param name="right">Ҫ�����ֵ��</param>
			/// <returns>����Ľ����</returns>
			public override object Compute(object right) {
				if (Name == "!") {
					if (right is bool)
						return !(bool)right;
					if (right is int)
						return (int)right == 0;
					if (right is double)
						return (double)right == 0;
					return right.ToString().Length == 0;
				}
				return OnError(right);
			}


			#endregion

			#region ˫Ŀ����

			/// <summary>
			/// ������ʽ��ֵ��
			/// </summary>
			/// <param name="left">��ֵ��</param>
			/// <param name="right">��ֵ��</param>
			/// <returns>ֵ��</returns>
			/// <exception cref="DivideByZeroException">�� 0 ����</exception>
			/// <exception cref="SyntaxException">��֧�ֵ��������</exception>
			object Compute(double left, double right) {
				switch (Name) {
					case "=":
					case "==":
						return (left == right);
					case "!=":
						return (left != right);
					case ">":
						return (left.CompareTo(right) > 0);
					case ">=":
						return (left.CompareTo(right) >= 0);
					case "<":
						return (left.CompareTo(right) < 0);
					case "<=":
						return (left.CompareTo(right) <= 0);
					case "||":
						return left != 0 || right != 0;
					case "&&":
						return left != 0 && right != 0;
					default:
						return OnError(left, right);
				}
			}

			/// <summary>
			/// ������ʽ��ֵ��
			/// </summary>
			///<param name="left">��ֵ��</param>
			/// <param name="right">��ֵ��</param>
			/// <returns>ֵ��</returns>
			object Compute(bool left, bool right) {
				switch (Name) {

					case "||":
					case "|":
						return left || right;
					case "&&":
					case "&":
						return left && right;
					case ">":
						return left.CompareTo(right) > 0;
					case ">=":
						return left.CompareTo(right) >= 0;
					case "<":
						return left.CompareTo(right) < 0;
					case "<=":
						return left.CompareTo(right) <= 0;
					case "=":
					case "==":
						return left == right;
					case "!=":
						return left != right;
					default:
						return OnError(left, right);
				}
			}

			/// <summary>
			/// ������ʽ��ֵ��
			/// </summary>
			/// <param name="left">��ֵ��</param>
			/// <param name="right">��ֵ��</param>
			/// <returns>ֵ��</returns>
			object Compute(string left, string right) {
				switch (Name) {
					case "=":
					case "==":
						return (left == right);
					case "~=":
						return left.Equals(right, StringComparison.OrdinalIgnoreCase);
					case "!=":
						return (left != right);
					case ">":
						return (String.Compare(left, right, StringComparison.Ordinal) > 0);
					case ">=":
						return (String.Compare(left, right, StringComparison.Ordinal) >= 0);
					case "<":
						return (String.Compare(left, right, StringComparison.Ordinal) < 0);
					case "<=":
						return (String.Compare(left, right, StringComparison.Ordinal) <= 0);
					default:
						return OnError(left, right);
				}
			}

			/// <summary>
			/// ������ʽ��ֵ��
			/// </summary>
			/// <param name="left">��ֵ��</param>
			/// <param name="right">��ֵ��</param>
			/// <returns>ֵ��</returns>
			/// <exception cref="DivideByZeroException">�� 0 ����</exception>
			/// <exception cref="SyntaxException">��֧�ֵ��������</exception>
			object Compute(int left, int right) {

				switch (Name) {
					case "=":
					case "==":
						return (left == right);
					case "!=":
						return (left != right);
					case ">":
						return (left.CompareTo(right) > 0);
					case ">=":
						return (left.CompareTo(right) >= 0);
					case "<":
						return (left.CompareTo(right) < 0);
					case "<=":
						return (left.CompareTo(right) <= 0);

					case ">>":
						return left >> right;
					case "<<":
						return left << right;
					case "|":
						return left | right;
					case "&":
						return left & right;
					case "^":
						return left ^ right;
					case "||":
						return left != 0 || right != 0;
					case "&&":
						return left != 0 && right != 0;
					default:
						return OnError(left, right);
				}
			}


			/// <summary>
			/// ͨ����ǰ������������ʽ��ֵ��
			/// </summary>
			/// <param name="left">��ֵ��</param>
			/// <param name="right">��ֵ��</param>
			/// <returns>ֵ��</returns>
			/// <exception cref="SyntaxException">�޷����㡣</exception>
			public override object Compute(object left, object right) {
				if (left is bool)
					if (right is bool)
						return Compute((bool)left, (bool)right);
					else
						left = (bool)left ? 1 : 0;
				if (left is int)
					if (right is int)
						return Compute((int)left, (int)right);
					else
						left = (double)left;
				if (left is double)
					if (right is int || right is double)
						return Compute((double)left, (double)right);

				return Compute(right.ToString(), left.ToString());

			}

			#endregion

		}

		/// <summary>
		/// ��ʾλ�����Ĳ�������
		/// </summary>
		public class BitOperator : Operator {

			/// <summary>
			/// ��ȡָʾ��ǰ�������Ƿ��ǵ�Ŀ�����Ĳ���ֵ��
			/// </summary>
			/// <value></value>
			public override bool IsSingle {
				get {
					return false;
				}
			}

			/// <summary>
			/// ��ʼ�� <see cref="CorePlus.RunTime.Arithmetic.BitOperator"/> ����ʵ����
			/// </summary>
			/// <param name="name">���֡�</param>
			/// <param name="priority">���ȵȼ���</param>
			/// <param name="intC">���ؼ�������ί�С�</param>
			public BitOperator(string name, int priority, Func<int, int, int> intC)
				: base(name, priority) {
				_getter = intC;
			}

			Func<int, int, int> _getter;

			/// <summary>
			/// ͨ����ǰ������������ʽ��ֵ��
			/// </summary>
			/// <param name="right">Ҫ�����ֵ��</param>
			/// <returns>����Ľ����</returns>
			public override object Compute(object right) {
				return OnError(right);
			}

			/// <summary>
			/// ͨ����ǰ������������ʽ��ֵ��
			/// </summary>
			/// <param name="left">��ֵ��</param>
			/// <param name="right">��ֵ��</param>
			/// <returns>ֵ��</returns>
			/// <exception cref="SyntaxException">�޷����㡣</exception>
			public override object Compute(object left, object right) {
				if (left is bool)
					left = (bool)left ? 1 : 0;
				if (right is bool)
					right = (bool)right ? 1 : 0;
				try {
					return _getter((int)left, (int)right);
				} catch (InvalidCastException) {
					return OnError(left, right);
				}

			}

		}

		/// <summary>
		/// ��ʾ�����ʽ�Ĳ�������
		/// </summary>
		public class SingleOperator : Operator {

			/// <summary>
			/// ��ʼ�� <see cref="CorePlus.RunTime.Arithmetic.SingleOperator"/> ����ʵ����
			/// </summary>
			/// <param name="name">���֡�</param>
			/// <param name="priority">���ȵȼ���</param>
			public SingleOperator(string name, int priority)
				: base(name, priority) {
			}



			/// <summary>
			/// ͨ����ǰ������������ʽ��ֵ��
			/// </summary>
			/// <param name="right">Ҫ�����ֵ��</param>
			/// <returns>����Ľ����</returns>
			public override object Compute(object right) {

				switch (Name) {
					case "++":
						if (right is int)
							return (int)right + 1;
						if (right is double)
							return (double)right + 1;
						goto default;
					case "--":
						if (right is int)
							return (int)right - 1;
						if (right is double)
							return (double)right - 1;
						goto default;
					case "~":
						try {
							return ~(int)right;
						} catch (InvalidCastException) {
							goto default;
						}
					default:
						return OnError(right);

				}
			}

			/// <summary>
			/// ��ȡָʾ��ǰ�������Ƿ��ǵ�Ŀ�����Ĳ���ֵ��
			/// </summary>
			/// <value></value>
			public override bool IsSingle {
				get {
					return true;
				}
			}


			/// <summary>
			/// ͨ����ǰ������������ʽ��ֵ��
			/// </summary>
			/// <param name="left">��ֵ��</param>
			/// <param name="right">��ֵ��</param>
			/// <returns>ֵ��</returns>
			/// <exception cref="SyntaxException">�޷����㡣</exception>
			public override object Compute(object left, object right) {
				return OnError(left, right);
			}

		}

		/// <summary>
		/// ��ʾ����һ�������Ĳ�������
		/// </summary>
		public class FunctionCallOperator : SingleOperator {

			/// <summary>
			/// ��ʼ�� <see cref="CorePlus.RunTime.Arithmetic.FunctionCallOperator"/> ����ʵ����
			/// </summary>
			/// <param name="name">���֡�</param>
			/// <param name="fn">�����ί�С�</param>
			public FunctionCallOperator(string name, Func<double, double> fn)
				: base(name, 3) {
				_getter = fn;
			}

			Func<double, double> _getter;

			/// <summary>
			/// ͨ����ǰ������������ʽ��ֵ��
			/// </summary>
			/// <param name="right">Ҫ�����ֵ��</param>
			/// <returns>����Ľ����</returns>
			public override object Compute(object right) {

				if (right is int)
					return _getter((double)(int)right);
				if (right is double)
					return _getter((double)right);
				if (right is bool) {
					return _getter((bool)right ? 1.0 : 0.0);
				}
				return OnError(right);
			}

		}

		/// <summary>
		/// ��ʾ������������
		/// </summary>
		public class OtherOperator : Operator {

			/// <summary>
			/// ��ʼ�� <see cref="CorePlus.RunTime.Arithmetic.OtherOperator"/> ����ʵ����
			/// </summary>
			/// <param name="name">���֡�</param>
			/// <param name="priority">���ȵȼ���</param>
			public OtherOperator(string name, int priority)
				: base(name, priority) {
			}

			/// <summary>
			/// ��ȡָʾ��ǰ�������Ƿ��ǵ�Ŀ�����Ĳ���ֵ��
			/// </summary>
			/// <value></value>
			public override bool IsSingle {
				get {
					return false;
				}
			}

			/// <summary>
			/// ͨ����ǰ������������ʽ��ֵ��
			/// </summary>
			/// <param name="right">Ҫ�����ֵ��</param>
			/// <returns>����Ľ����</returns>
			public override object Compute(object right) {

				return OnError(right);
			}

			/// <summary>
			/// ͨ����ǰ������������ʽ��ֵ��
			/// </summary>
			/// <param name="left">��ֵ��</param>
			/// <param name="right">��ֵ��</param>
			/// <returns>ֵ��</returns>
			/// <exception cref="SyntaxException">�޷����㡣</exception>
			public override object Compute(object left, object right) {
				return OnError(left, right);
			}
		}

		/// <summary>
		/// ���������顣
		/// </summary>
		List<Operator> _operators;

		/// <summary>
		/// ��ȡ֧�ֵĲ�������
		/// </summary>
		/// <exception cref="ArgumentNullException"><paramref name="value"/> Ϊ�ա�</exception>
		public List<Operator> Operators {
			get {
				return _operators;
			}
		}

		/// <summary>
		/// ��ʼ����
		/// </summary>
		void Setup() {
			_operators = new List<Operator> {
				new OtherOperator("(", 3),
				new OtherOperator(")", 3),
				new ArithmeticOperator("*", 7, (a, b)=> a * b, (a, b)=> a * b),
				new ArithmeticOperator("/", 7, (a, b)=> a / b, (a, b)=> a / b),

				new ArithmeticOperator("+", 10, (a, b)=> a + b, (a, b)=> a + b),
				new ArithmeticOperator("-", 10, (a, b)=> a - b, (a, b)=> a - b),
				new ArithmeticOperator("\\", 10, (a, b)=> a / b, (a, b)=> (double) ( (int) (a / b - a % b / b ) )),
				new ArithmeticOperator("^", 5, (a, b)=> (int)Math.Pow((double)a, (double)b), Math.Pow),

				new BoolOperator(">", 20),
				new BoolOperator(">=", 20),
				new BoolOperator("<", 20),
				new BoolOperator("<=", 20),
				new BoolOperator("!=", 20),
				new BoolOperator("==", 20),
				new BoolOperator("~=", 20),

				new BoolOperator("!", 4),
				new BoolOperator("||", 42),
				new BoolOperator("&&", 43),

				new SingleOperator("++", 2),
				new SingleOperator("--", 2),
				new BitOperator("&", 40, (a, b) => a & b),
				new SingleOperator("~", 4),
				new BitOperator("|", 40, (a, b) => a | b),
				new BitOperator(">>", 40, (a, b) => a >> b),
				new BitOperator("<<", 40, (a, b) => a << b),

				new ArithmeticOperator("%", 7, (a, b)=> a % b, (a, b)=> a % b),

				new FunctionCallOperator("sin", Math.Sin),
				new FunctionCallOperator("cos", Math.Cos),
				new FunctionCallOperator("tan", Math.Tan),
				new FunctionCallOperator("tg", Math.Tan),
				new FunctionCallOperator("cot", d => 1 / Math.Tan(d)),
				new FunctionCallOperator("ctg", d => 1 / Math.Tan(d)),
				new FunctionCallOperator("sinh", Math.Sinh),
				new FunctionCallOperator("cosh", Math.Cosh),
				new FunctionCallOperator("tanh", Math.Tanh),
				new FunctionCallOperator("sqrt", Math.Sqrt),
				new FunctionCallOperator("round", Math.Round),
				new FunctionCallOperator("lg", Math.Log10),
				new FunctionCallOperator("ln", Math.Log),
				new FunctionCallOperator("exp", Math.Exp),
				new FunctionCallOperator("floor", Math.Floor),
				new FunctionCallOperator("ceil", Math.Ceiling),
				new FunctionCallOperator("abs", Math.Abs),
				new FunctionCallOperator("arccos", Math.Acos),
				new FunctionCallOperator("arcsin", Math.Asin),
				new FunctionCallOperator("arctan", Math.Atan),
				new FunctionCallOperator("int", Math.Truncate)
			};

            UpdateOperators();

			_operators.TrimExcess();


		}

        /// <summary>
        /// ���»���Ĳ������б�
        /// </summary>
        public void UpdateOperators() {

            _operators.Sort((a, b) => a.Name.CompareTo(b.Name));
        }

		/// <summary>
		/// �������ֻ�ȡ��������
		/// </summary>
		/// <param name="name">���֡�</param>
		/// <returns>��������</returns>
		/// <exception cref="SyntaxException">�Ҳ�����Ҫ�Ĳ�������</exception>
		Operator GetOperators(string name) {
			int middle, t;
			int start = 0, end = _operators.Count;

			end--;


			while (start <= end) {
				middle = (start + end) / 2;

				t = _operators[middle].Name.CompareTo( name );

				if (t < 0)
					start = middle + 1;
				else if (t > 0)
					end = middle - 1;
				else
					return _operators[middle];
			}

			throw new SyntaxException("��֧�ֵĺ���������� " + name, SyntaxErrorType.Invalid);
		}

		#endregion

		#region ����

		/// <summary>
		/// ������ʽ��
		/// </summary>
		/// <param name="expression">���ʽ��</param>
		/// <returns>����Ľ����</returns>
		/// <exception cref="SyntaxException">���ʽ�������������</exception>
		/// <exception cref="ArithmeticException">���ʽ�������������</exception>
		/// <exception cref="ArgumentNullException">���ʽ�ǿ��ַ�����</exception>
		/// <exception cref="DivideByZeroException">�� 0 ����</exception>
		public static object ComputeExpression(string expression) {
			return new Arithmetic().Compute(expression);
		}

		#endregion
	}
}
