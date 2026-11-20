import { createApp } from 'vue'

import AppRoot from './AppRoot.vue'
import router from './router'
import treatmentCodeImportRoute from './router/treatmentCodeImportRoute'

import './assets/main.css'

router.addRoute(treatmentCodeImportRoute)

const app = createApp(AppRoot)
app.use(router)

app.mount('#app')

