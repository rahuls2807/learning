import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const requestFailures = new Rate('request_failures');
const baseUrl = (__ENV.BASE_URL || 'http://localhost:5156').replace(/\/$/, '');

export const options = {
  scenarios: {
    ramp: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '30s', target: 5 },
        { duration: '1m', target: 20 },
        { duration: '1m', target: 20 },
        { duration: '30s', target: 0 },
      ],
      gracefulRampDown: '10s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.02'],
    http_req_duration: ['p(95)<1500'],
    request_failures: ['rate<0.02'],
  },
};

export default function () {
  const responses = http.batch([
    ['GET', `${baseUrl}/`, null, { tags: { endpoint: 'home' } }],
    ['GET', `${baseUrl}/Client/BookWorker?page=1`, null, { tags: { endpoint: 'worker-search' } }],
    ['GET', `${baseUrl}/health/ready`, null, { tags: { endpoint: 'readiness' } }],
  ]);

  responses.forEach((response) => {
    const passed = check(response, {
      'returns HTTP 200': (r) => r.status === 200,
    });
    requestFailures.add(!passed);
  });

  sleep(1);
}
