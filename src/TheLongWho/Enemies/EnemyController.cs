using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheLongWho.Enemies
{
	public abstract class EnemyController : MonoBehaviour
	{
		public abstract string Name { get; }
	}
}
