import React, { useEffect, useState } from 'react';
import service from './service.js';
import Login from './components/Login.js';
import Register from './components/Register.js';

function App() {
  const [newTodo, setNewTodo] = useState("");
  const [todos, setTodos] = useState([]);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [user, setUser] = useState(null);
  const [authMode, setAuthMode] = useState('login'); // 'login' או 'register'

  // בדיקה אם יש token שמור
  useEffect(() => {
    const token = localStorage.getItem('token');
    const savedUser = localStorage.getItem('user');
    
    if (token && savedUser) {
      setIsAuthenticated(true);
      setUser(JSON.parse(savedUser));
    }

    // האזנה לאירוע 401 מה-interceptor
    const handleUnauthorized = () => {
      setIsAuthenticated(false);
      setUser(null);
      setTodos([]);
    };

    window.addEventListener('unauthorized', handleUnauthorized);
    return () => window.removeEventListener('unauthorized', handleUnauthorized);
  }, []);

  // טעינת המשימות רק אם מחובר
  useEffect(() => {
    if (isAuthenticated) {
      getTodos();
    }
  }, [isAuthenticated]);

  async function getTodos() {
    try {
      const todos = await service.getTasks();
      setTodos(todos);
    } catch (error) {
      console.error('Error fetching todos:', error);
    }
  }

  async function createTodo(e) {
    e.preventDefault();
    if (!newTodo.trim()) return;
    
    try {
      await service.addTask(newTodo);
      setNewTodo("");//clear input
      await getTodos();//refresh tasks list (in order to see the new one)
    } catch (error) {
      console.error('Error creating todo:', error);
    }
  }

  async function updateCompleted(todo, isComplete) {
    try {
      await service.setCompleted(todo.id, isComplete);
      await getTodos();//refresh tasks list (in order to see the updated one)
    } catch (error) {
      console.error('Error updating todo:', error);
    }
  }

  async function deleteTodo(id) {
    try {
      await service.deleteTask(id);
      await getTodos();//refresh tasks list
    } catch (error) {
      console.error('Error deleting todo:', error);
    }
  }

  const handleLogin = (token, userData) => {
    setIsAuthenticated(true);
    setUser(userData);
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setIsAuthenticated(false);
    setUser(null);
    setTodos([]);
  };

  // אם לא מחובר - הצג דף התחברות/הרשמה
  if (!isAuthenticated) {
    return authMode === 'login' ? (
      <Login 
        onLogin={handleLogin}
        switchToRegister={() => setAuthMode('register')}
      />
    ) : (
      <Register 
        onLogin={handleLogin}
        switchToLogin={() => setAuthMode('login')}
      />
    );
  }

  // אם מחובר - הצג את אפליקציית Todo
  return (
    <section className="todoapp">
      <header className="header">
        <div className="user-info">
          <span>שלום, {user.username}!</span>
          <button onClick={handleLogout} className="logout-btn">
            התנתק
          </button>
        </div>
        <h1>todos</h1>
        <form onSubmit={createTodo}>
          <input className="new-todo" placeholder="Well, let's take on the day" value={newTodo} onChange={(e) => setNewTodo(e.target.value)} />
        </form>
      </header>
      <section className="main" style={{ display: "block" }}>
        <ul className="todo-list">
          {todos.map(todo => {
            return (
              <li className={todo.isComplete ? "completed" : ""} key={todo.id}>
                <div className="view">
                  <input className="toggle" type="checkbox" defaultChecked={todo.isComplete} onChange={(e) => updateCompleted(todo, e.target.checked)} />
                  <label>{todo.name}</label>
                  <button className="destroy" onClick={() => deleteTodo(todo.id)}></button>
                </div>
              </li>
            );
          })}
        </ul>
      </section>
    </section >
  );
}

export default App;