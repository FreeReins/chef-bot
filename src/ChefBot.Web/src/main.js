import { mount } from 'svelte';
import App from './App.svelte';
import './styles.css';

// Straight into <body>: the layout in styles.css centres the body itself.
mount(App, { target: document.body });
