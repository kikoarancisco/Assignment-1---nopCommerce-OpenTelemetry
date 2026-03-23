import http from 'k6/http';
import { sleep } from 'k6';

export const options = {
  vus: 10,           // 10 Users 
  duration: '2m',    // For 2 minutes
};

export default function () {
  const baseUrl = 'http://localhost';

  // Visit home page
  http.get(`${baseUrl}/`);
  sleep(1);

  // Go to computers page
  http.get(`${baseUrl}/computers`);
  sleep(1);

  // Check the cart
  http.get(`${baseUrl}/cart`);
  sleep(1);
  
  // Try to checkout
  http.get(`${baseUrl}/checkout`);
  sleep(2);
}