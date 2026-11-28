import { createApp } from 'vue'

import AppRoot from './AppRoot.vue'
import router from './router'

import './assets/main.css'

const app = createApp(AppRoot)
app.use(router)

app.mount('#app')

