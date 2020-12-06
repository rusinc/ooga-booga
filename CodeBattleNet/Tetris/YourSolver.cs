﻿/*-
 * #%L
 * Codenjoy - it's a dojo-like platform from developers to developers.
 * %%
 * Copyright (C) 2018 Codenjoy
 * %%
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public
 * License along with this program.  If not, see
 * <http://www.gnu.org/licenses/gpl-3.0.html>.
 * #L%
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using TetrisClient.CustomLogic;

namespace TetrisClient
{
	/// <summary>
	/// В этом классе находится логика Вашего бота
	/// </summary>
	internal class YourSolver : AbstractSolver
	{
		private Oracul oracul;

		public YourSolver(string server)
			: base(server)
		{

			oracul = new Oracul();

		}

		/// <summary>
		/// Этот метод вызывается каждый игровой тик
		/// </summary>
		protected internal override Command Get(Board board)
		{
			// Код писать сюда!
			//return Command.ROTATE_CLOCKWISE_90;
			oracul.updateInfo(board);

			Task<Command> task = oracul.getTheBestCommand();
			
			task.Wait();

			return task.Result;
		}

		
	}
}
