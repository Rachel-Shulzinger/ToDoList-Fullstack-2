import axios from 'axios';

// הגדרת Config Defaults - כתובת בסיס לכל הקריאות
const API_URL = process.env.REACT_APP_API_URL || "http://localhost:5213";
axios.defaults.baseURL = API_URL;
axios.defaults.headers.common['Content-Type'] = 'application/json';

// הוספת Request Interceptor להוספת JWT token
axios.interceptors.request.use(
  function (config) {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    console.log('🚀 API Request:', config.method?.toUpperCase(), config.url, token ? '(with token)' : '(no token)');
    return config;
  },
  function (error) {
    return Promise.reject(error);
  }
);

// הוספת Response Interceptor לטיפול בשגיאות (כולל 401)
axios.interceptors.response.use(
  // פונקציה שרצה כשהתגובה הצליחה (status 2xx)
  function (response) {
    console.log('✅ API Response Success:', response.config.method?.toUpperCase(), response.config.url, response.status);
    return response;
  },
  // פונקציה שרצה כשיש שגיאה
  function (error) {
    console.error('❌ API Response Error:', {
      method: error.config?.method?.toUpperCase(),
      url: error.config?.url,
      status: error.response?.status,
      statusText: error.response?.statusText,
      message: error.message,
      data: error.response?.data
    });
    
    // טיפול ב-401 Unauthorized - מעבר ללוגין
    if (error.response?.status === 401) {
      console.warn('� Unauthorized (401) - Redirecting to login');
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      // אירוע מותאם אישית שה-App יקשיב לו
      window.dispatchEvent(new CustomEvent('unauthorized'));
    } else if (error.response?.status === 404) {
      console.warn('� Resource not found (404)');
    } else if (error.response?.status >= 500) {
      console.error('� Server error (5xx)');
    }
    
    return Promise.reject(error);
  }
);

const apiUrl = API_URL // נשתמש במשתנה הסביבה

export default {
  getTasks: async () => {
    const result = await axios.get(`${apiUrl}/items`)    
    return result.data;
  },

  addTask: async(name)=>{
    const result = await axios.post(`${apiUrl}/items`, {
      name: name,
      isComplete: false
    });
    return result.data;
  },

  setCompleted: async(id, isComplete)=>{
    const result = await axios.put(`${apiUrl}/items/${id}`, {
      name: '', // נצטרך לקבל את השם הקיים
      isComplete: isComplete
    });
    return result.data;
  },

  deleteTask: async(id)=>{
    await axios.delete(`${apiUrl}/items/${id}`);
    return true;
  },

  // פונקציות Authentication חדשות
  login: async(username, password) => {
    const result = await axios.post('/auth/login', { username, password });
    return result.data;
  },

  register: async(username, password) => {
    const result = await axios.post('/auth/register', { username, password });
    return result.data;
  }
};
