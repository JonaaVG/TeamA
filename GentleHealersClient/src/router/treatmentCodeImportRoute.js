import { canImportTreatmentCodes, getTreatmentCodeClaims } from '@/features/treatmentCodes/access'

export default {
  path: '/admin/treatment-codes/import',
  name: 'treatment-code-import',
  component: () => import('../views/TreatmentCodeImportPage.vue'),
  meta: {
    auth: true,
    standaloneLayout: true,
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
}
