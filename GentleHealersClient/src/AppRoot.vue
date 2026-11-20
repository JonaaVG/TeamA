<script setup>
import { RouterView } from 'vue-router'
import App from './App.vue'
</script>

<template>
  <RouterView v-if="$route.meta.standaloneLayout" />
  <App v-else />
</template>
