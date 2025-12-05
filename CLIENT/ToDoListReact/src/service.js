import axios from 'axios';

// הגדרת Config Defaults - כתובת בסיס לכל הקריאות
const API_URL = process.env.REACT_APP_API_URL || "http://localhost:5213";
axios.defaults.baseURL = API_URL;
axios.defaults.headers.common['Content-Type'] = 'application/json';

const apiUrl = API_URL;

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
    // קודם נקבל את המשימה הנוכחית כדי לשמור את השם
    const currentTodo = await axios.get(`${apiUrl}/items/${id}`);
    const result = await axios.put(`${apiUrl}/items/${id}`, {
      name: currentTodo.data.name,
      isComplete: isComplete
    });
    return result.data;
  },

  deleteTask: async(id)=>{
    await axios.delete(`${apiUrl}/items/${id}`);
    return true;
  }
};
