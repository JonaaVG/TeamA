import { createRouter, createWebHistory } from 'vue-router'
import DashboardPage from '@/views/DashboardPage.vue'
// Define the routes array first
const routes = [
  {
    path: '/',
    name: 'dashboard',
    component: DashboardPage,
    meta: {
      auth: true,
    }
  },
  {
    path: '/login',
    name: 'login',
    component: () => import('../views/LoginPage.vue'),
    meta: {
      auth: false,
    }
  }
  
]

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior(to, from, savedPosition) {
    if (savedPosition) {
      return savedPosition
    } else {
      return { top: 0 }
    }
  }
})
router.beforeEach((to, from, next) => {
  const isAuthenticated = !!localStorage.getItem('token') // Check if JWT token exists

  if (to.meta.auth && !isAuthenticated) {
    // Redirect to login if route requires authentication and user is not logged in
    next({ name: 'login' })
  } else if (to.name === 'login' && isAuthenticated) {
    // Redirect to dashboard if user is already logged in and tries to access login
    next({ name: 'dashboard' })
  } else {
    // Allow navigation
    next()
  }
})  
router.afterEach(() => {
  setTimeout(() => {
    const resetter = document.getElementById('focus-resetter')
    if (resetter) {
      resetter.focus()
    }
  }, 50)
})

export default router