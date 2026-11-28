import { createRouter, createWebHistory } from 'vue-router'
import { canImportTreatmentCodes, getTreatmentCodeClaims } from '@/features/treatmentCodes/access'
import DashboardPage from '@/views/DashboardPage.vue'
import {parseJwt} from '@/services/JwtParser.js'

// Define the routes array first
const routes = [
  {
    path: '/beheer/zorgbehandelingen/importeren',
    name: 'treatment-code-import',
    component: () => import('../views/TreatmentCodeImportPage.vue'),
    meta: {
      auth: true,
    },
    beforeEnter: () => {
      const token = localStorage.getItem('token')
      if (!getTreatmentCodeClaims(token)) {
        localStorage.removeItem('token')
        localStorage.removeItem('userRole')
        return { name: 'login' }
      }
      if (!canImportTreatmentCodes(token)) return { name: 'dashboard' }
      return true
    },
  },
  {
    path: '/admin/treatment-codes/import',
    redirect: { name: 'treatment-code-import' },
  },
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
    path: '/activeer',
    name: 'setuppassword',
    component: () => import('../views/Patient/SetUpPasswordPage.vue'),
    meta: {
      auth: false,
    }
  },
  {
    path: '/patient/aanmaken',
    name: 'addpatient',
    component: () => import('../views/Patient/AddPatientPage.vue'),
    meta: {
      auth: true,
      roles: ['GeneralPractitioner']
    }
  },

  {
    path: '/rol/management',
    name: 'rolManagement',
    component: () => import('../views/Systemadministrator/RolemanagementPage.vue'),
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
router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('token');
  const isAuthenticated = !!token;

  let userRole = '';
  if (token) {
    const decodedToken = parseJwt(token);
    
    userRole = decodedToken?.role || '';
  }

  if (to.meta.auth && !isAuthenticated) {
    // Redirect to login if route requires authentication and user is not logged in
    next({ name: 'login' })
  } else if (to.name === 'login' && isAuthenticated) {
    // Redirect to dashboard if user is already logged in and tries to access login
    next({ name: 'dashboard' })
  } else if (to.meta.roles && !to.meta.roles.includes(userRole)) {
    next({ name: 'accessDenied' })
  }else {
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
