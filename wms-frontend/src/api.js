import axios from 'axios';

const api = axios.create({
    // Đổi cổng 7123 này thành cổng thật trên Visual Studio của bạn nhé
    baseURL: 'https://localhost:7123/api', 
    headers: {
        'Content-Type': 'application/json'
    }
});

export default api;