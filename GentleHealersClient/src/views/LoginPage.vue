<template>
  <main class="flex min-h-screen items-center justify-center bg-[#e8f3ff] p-4 font-['Inter',system-ui,sans-serif]">
    <section
      class="w-full max-w-[530px] rounded-[20px] border border-[#a9bbd6] bg-white px-9 pb-6 pt-10"
      aria-labelledby="login-heading"
    >
      <!-- Logo (alleen tekst) -->
      <div class="text-center text-[1.75rem] font-bold tracking-tight" aria-label="Zachte Heelmeesters">
        <span class="text-[#12376b]">Zachte</span>
        <span class="text-[#0a76d8]">Heelmeesters</span>
      </div>
      <hr class="mb-6 mt-4 h-1 rounded-sm border-0 bg-[#e3eefb]" />

      <h1 id="login-heading" class="mb-6 text-left text-[1.6rem] font-bold text-[#12376b]">
        Inloggen
      </h1>

      <form @submit.prevent="handleLogin" novalidate>
        <!-- E-mailadres -->
        <div class="relative mb-3.5">
          <label for="email" class="sr-only">E-mailadres</label>
          <User class="pointer-events-none absolute left-4 top-1/2 size-6 -translate-y-1/2 fill-[#12376b] text-[#12376b]" aria-hidden="true" />
          <input
            id="email"
            type="text"
            v-model.trim="email"
            autocomplete="email"
            placeholder="E-mailadres"
            :aria-invalid="!!errorMessage"
            class="h-[3.1rem] w-full rounded-[14px] border-[3px] border-[#12376b] bg-white pl-14 pr-12 text-base text-[#12376b] placeholder:text-[#6b7a99] focus:outline-none focus:ring-[3px] focus:ring-[#0a76d8]/30"
          />
        </div>

        <!-- Wachtwoord -->
        <div class="relative mb-3.5">
          <label for="password" class="sr-only">Wachtwoord</label>
          <Lock class="pointer-events-none absolute left-4 top-1/2 size-6 -translate-y-1/2 fill-[#12376b] text-[#12376b]" aria-hidden="true" />
          <input
            id="password"
            :type="showPassword ? 'text' : 'password'"
            v-model="password"
            autocomplete="current-password"
            placeholder="Wachtwoord"
            :aria-invalid="!!errorMessage"
            class="h-[3.1rem] w-full rounded-[14px] border-[3px] border-[#12376b] bg-white pl-14 pr-12 text-base text-[#12376b] placeholder:text-[#6b7a99] focus:outline-none focus:ring-[3px] focus:ring-[#0a76d8]/30"
          />
          <button
            type="button"
            class="absolute right-3.5 top-1/2 -translate-y-1/2 text-[#7d8aa3]"
            :aria-label="showPassword ? 'Verberg wachtwoord' : 'Toon wachtwoord'"
            @click="showPassword = !showPassword"
          >
            <Eye v-if="showPassword" class="size-6" aria-hidden="true" />
            <EyeOff v-else class="size-6" aria-hidden="true" />
          </button>
        </div>

        <!-- Foutmelding -->
        <div
          v-if="errorMessage"
          class="mb-3.5 flex items-center gap-3 rounded-[10px] border border-[#8b1a1a] bg-[#f7e1e1] px-4 py-3 text-[0.95rem] font-bold leading-snug text-[#7a0e0e]"
          role="alert"
          aria-live="polite"
        >
          <CircleAlert class="size-6 shrink-0" aria-hidden="true" />
          <span>{{ errorMessage }}</span>
        </div>

        <button
          type="submit"
          :disabled="isLoading"
          class="flex h-[3.3rem] w-full items-center justify-center gap-2 rounded-[14px] bg-[#12376b] text-2xl font-semibold text-white transition-colors hover:bg-[#0d2a54] disabled:cursor-not-allowed disabled:opacity-60"
        >
          Inloggen
          <ArrowRight class="size-6" aria-hidden="true" />
        </button>
      </form>

      <router-link
        to="/wachtwoord-vergeten"
        class="mt-4 block text-center text-[0.95rem] text-[#0a76d8] underline"
      >
        Wachtwoord vergeten?
      </router-link>
    </section>
  </main>
</template>

<script setup>
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import api from '@/services/api';
import { User, Lock, Eye, EyeOff, CircleAlert, ArrowRight } from 'lucide-vue-next';

const router = useRouter();

const email = ref('');
const password = ref('');
const showPassword = ref(false);
const errorMessage = ref('');
const isLoading = ref(false);

const handleLogin = async () => {
  errorMessage.value = '';

  if (!email.value || !password.value) {
    errorMessage.value = 'Vul zowel je e-mailadres als wachtwoord in.';
    return;
  }

  isLoading.value = true;

  try {
    const response = await api.post('/auth/login', {
      email: email.value,
      password: password.value,
    });

    localStorage.setItem('token', response.data.token);
    router.push('/');
  } catch (error) {
    errorMessage.value =
      error.response?.data?.message ||
      'Inloggegevens onjuist. Controleer je E-mailadres en wachtwoord.';
  } finally {
    isLoading.value = false;
  }
};
</script>