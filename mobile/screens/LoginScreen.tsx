import React, { useState } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  Alert,
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  Dimensions,
} from 'react-native';
import { BlurView } from 'expo-blur';
import { LinearGradient } from 'expo-linear-gradient';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { RootStackParamList } from '../types';
import { supabase } from '../lib/supabase';

type Props = NativeStackScreenProps<RootStackParamList, 'Login'>;

const { width, height } = Dimensions.get('window');

export default function LoginScreen({ navigation }: Props) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [isFocused, setIsFocused] = useState(false);

  const handleLogin = async () => {
    if (!email || !password) {
      Alert.alert('Hata', 'E-posta ve şifre gerekli');
      return;
    }

    setLoading(true);
    try {
      const { data, error } = await supabase.auth.signInWithPassword({
        email,
        password,
      });

      if (error) throw error;

      if (data.user) {
        const { data: profile } = await supabase
          .from('profiles')
          .select('*')
          .eq('id', data.user.id)
          .single();

        if (profile && profile.role === 'waiter') {
          navigation.replace('Home');
        } else {
          Alert.alert('Hata', 'Bu uygulama sadece garsonlar içindir');
          await supabase.auth.signOut();
        }
      }
    } catch (error: any) {
      Alert.alert('Giriş Hatası', error.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <KeyboardAvoidingView 
      style={styles.container} 
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      {/* Animated Gradient Background */}
      <LinearGradient
        colors={['#0A0E27', '#1A1F3A', '#2D1B4E', '#1A1F3A', '#0A0E27']}
        locations={[0, 0.25, 0.5, 0.75, 1]}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 1 }}
        style={StyleSheet.absoluteFill}
      />

      {/* Radial Light Burst - Top */}
      <View style={styles.lightBurst}>
        <LinearGradient
          colors={['rgba(100, 150, 255, 0.15)', 'transparent']}
          style={styles.radialGradient}
        />
      </View>

      {/* Content */}
      <View style={styles.content}>
        {/* Logo - Hidden when focused */}
        {!isFocused && (
          <View style={styles.logoSection}>
            <Text style={styles.logo}>Ordevo</Text>
            <Text style={styles.subtitle}>Garson Uygulaması</Text>
          </View>
        )}

        {/* Liquid Glass Container */}
        <View style={styles.glassContainer}>
          <BlurView intensity={40} tint="light" style={styles.glassBlur}>
            {/* Inner Glow */}
            <LinearGradient
              colors={[
                'rgba(255, 255, 255, 0.25)',
                'rgba(255, 255, 255, 0.05)',
                'rgba(255, 255, 255, 0.15)',
              ]}
              locations={[0, 0.5, 1]}
              style={styles.innerGlow}
            />

            <View style={styles.glassContent}>
              {/* Email Input */}
              <View style={styles.inputContainer}>
                <BlurView intensity={30} tint="light" style={styles.inputBlur}>
                  <TextInput
                    style={styles.input}
                    placeholder="E-posta"
                    placeholderTextColor="rgba(255, 255, 255, 0.4)"
                    value={email}
                    onChangeText={setEmail}
                    autoCapitalize="none"
                    keyboardType="email-address"
                    onFocus={() => setIsFocused(true)}
                    onBlur={() => setIsFocused(false)}
                  />
                </BlurView>
              </View>

              {/* Password Input */}
              <View style={styles.inputContainer}>
                <BlurView intensity={30} tint="light" style={styles.inputBlur}>
                  <TextInput
                    style={styles.input}
                    placeholder="Şifre"
                    placeholderTextColor="rgba(255, 255, 255, 0.4)"
                    value={password}
                    onChangeText={setPassword}
                    secureTextEntry
                    onFocus={() => setIsFocused(true)}
                    onBlur={() => setIsFocused(false)}
                  />
                </BlurView>
              </View>

              {/* Login Button */}
              <TouchableOpacity
                style={styles.buttonContainer}
                onPress={handleLogin}
                disabled={loading}
                activeOpacity={0.85}
              >
                <LinearGradient
                  colors={['#007AFF', '#0051D5']}
                  start={{ x: 0, y: 0 }}
                  end={{ x: 1, y: 1 }}
                  style={styles.button}
                >
                  {loading ? (
                    <ActivityIndicator color="#FFFFFF" />
                  ) : (
                    <Text style={styles.buttonText}>Giriş Yap</Text>
                  )}
                </LinearGradient>
              </TouchableOpacity>
            </View>
          </BlurView>

          {/* Glass Border Effect */}
          <View style={styles.glassBorder} />
        </View>

        {/* Footer */}
        <Text style={styles.footer}>Version 1.0.0</Text>
      </View>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  lightBurst: {
    position: 'absolute',
    top: -height * 0.3,
    left: width * 0.2,
    width: width * 1.5,
    height: height * 0.8,
  },
  radialGradient: {
    flex: 1,
    borderRadius: width,
  },
  content: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 32,
  },
  logoSection: {
    alignItems: 'center',
    marginBottom: 64,
  },
  logo: {
    fontSize: 56,
    fontWeight: '700',
    color: '#FFFFFF',
    letterSpacing: -1,
    marginBottom: 8,
  },
  subtitle: {
    fontSize: 15,
    fontWeight: '500',
    color: 'rgba(255, 255, 255, 0.6)',
    letterSpacing: 3,
    textTransform: 'uppercase',
  },
  glassContainer: {
    width: '100%',
    maxWidth: 400,
    position: 'relative',
  },
  glassBlur: {
    borderRadius: 30,
    overflow: 'hidden',
    backgroundColor: 'rgba(255, 255, 255, 0.08)',
    borderWidth: 1.5,
    borderColor: 'rgba(255, 255, 255, 0.18)',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 24 },
    shadowOpacity: 0.5,
    shadowRadius: 40,
    elevation: 20,
  },
  innerGlow: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    height: '50%',
    borderTopLeftRadius: 30,
    borderTopRightRadius: 30,
  },
  glassBorder: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    borderRadius: 30,
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.25)',
    pointerEvents: 'none',
  },
  glassContent: {
    padding: 40,
  },
  inputContainer: {
    marginBottom: 20,
  },
  inputBlur: {
    borderRadius: 16,
    overflow: 'hidden',
    backgroundColor: 'rgba(255, 255, 255, 0.05)',
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.15)',
  },
  input: {
    padding: 20,
    fontSize: 17,
    fontWeight: '500',
    color: '#FFFFFF',
    letterSpacing: 0.3,
  },
  buttonContainer: {
    marginTop: 12,
    borderRadius: 16,
    overflow: 'hidden',
    shadowColor: '#007AFF',
    shadowOffset: { width: 0, height: 12 },
    shadowOpacity: 0.5,
    shadowRadius: 20,
    elevation: 10,
  },
  button: {
    paddingVertical: 20,
    alignItems: 'center',
    justifyContent: 'center',
  },
  buttonText: {
    fontSize: 18,
    fontWeight: '600',
    color: '#FFFFFF',
    letterSpacing: 0.5,
  },
  footer: {
    marginTop: 48,
    fontSize: 13,
    fontWeight: '500',
    color: 'rgba(255, 255, 255, 0.35)',
    letterSpacing: 1,
  },
});
