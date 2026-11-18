import { createRouter, createWebHistory } from 'vue-router'
import DashboardPage from '@/views/DashboardPage.vue'
import { getTokenClaims } from '@/utils/auth' 
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
  },

  {
    path: '/rolManagement',
    name: 'rolManagement',
    component: () => import('../views/RolemanagementPage.vue'),
    meta: {
      auth: true,
      roles: ['SystemAdministrator'],
    }
  },

  {
  path: '/access-denied',
  name: 'accessDenied',
  component: () => import('../views/AccessDeniedPage.vue'),
  meta: { auth: true },
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

router.beforeEach((to) => {
  const claims = getTokenClaims()

  if (to.meta.auth && !claims) {
    localStorage.removeItem('token')
    return { name: 'login' }
  }

  if (to.name === 'login' && claims) {
    return { name: 'dashboard' }
  }

  const role =
    claims?.role ??
    claims?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']

  if (to.meta.roles && !to.meta.roles.includes(role)) {
    return { name: 'accessDenied' }
  }

  return true
})
// router.beforeEach((to, from, next) => {
//   const isAuthenticated = !!localStorage.getItem('token') // Check if JWT token exists

//   if (to.meta.auth && !isAuthenticated) {
//     // Redirect to login if route requires authentication and user is not logged in
//     next({ name: 'login' })
//   } else if (to.name === 'login' && isAuthenticated) {
//     // Redirect to dashboard if user is already logged in and tries to access login
//     next({ name: 'dashboard' })
//   } else {
//     // Allow navigation
//     next()
//   }
// })  
router.afterEach(() => {
  setTimeout(() => {
    const resetter = document.getElementById('focus-resetter')
    if (resetter) {
      resetter.focus()
    }
  }, 50)
})

export default router