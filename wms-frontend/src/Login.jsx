import React, { useState } from 'react';
import { TextField, Button, Box, Typography, Alert } from '@mui/material';
import api from './api';

export default function Login() {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [message, setMessage] = useState('');
    const [error, setError] = useState(false);

    const handleLogin = async (e) => {
        e.preventDefault();
        try {
            // Gọi api/Auth/login bên C#
            const response = await api.post('/Auth/login', {
                Username: username,
                Password: password
            });
            
            // Lấy dữ liệu user từ Backend trả về
            const userData = response.data;
            setMessage(`Đăng nhập thành công! Chào ${userData.fullName} (${userData.role})`);
            setError(false);
            
            // Thực tế ở đây ta sẽ lưu UserData vào LocalStorage và chuyển trang (navigate)
            console.log(userData);

        } catch (err) {
            setError(true);
            setMessage(err.response?.data?.message || "Lỗi kết nối máy chủ!");
        }
    };

    return (
        <Box sx={{ width: 300, margin: '100px auto', textAlign: 'center' }}>
            <Typography variant="h4" gutterBottom>WMS Login</Typography>
            
            {message && <Alert severity={error ? "error" : "success"} sx={{ mb: 2 }}>{message}</Alert>}

            <form onSubmit={handleLogin}>
                <TextField 
                    fullWidth label="Tên đăng nhập" variant="outlined" margin="normal"
                    value={username} onChange={(e) => setUsername(e.target.value)} 
                />
                <TextField 
                    fullWidth label="Mật khẩu" type="password" variant="outlined" margin="normal"
                    value={password} onChange={(e) => setPassword(e.target.value)} 
                />
                <Button fullWidth type="submit" variant="contained" size="large" sx={{ mt: 2 }}>
                    ĐĂNG NHẬP
                </Button>
            </form>
        </Box>
    );
}