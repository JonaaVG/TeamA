<script setup>
import { RouterView } from 'vue-router'
import App from './App.vue'
import ToastContainer from './features/shared/ToastContainer.vue'
</script>

<template>
  <RouterView v-if="$route.meta.standaloneLayout" />
  <App v-else />
  <ToastContainer />
</template>
