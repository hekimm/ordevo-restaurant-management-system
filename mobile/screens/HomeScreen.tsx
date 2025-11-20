import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  Alert,
  Dimensions,
  SafeAreaView,
} from 'react-native';
import { BlurView } from 'expo-blur';
import { LinearGradient } from 'expo-linear-gradient';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { RootStackParamList } from '../types';
import { supabase } from '../lib/supabase';

type Props = NativeStackScreenProps<RootStackParamList, 'Home'>;

const { width, height } = Dimensions.get('window');

export default function HomeScreen({ navigation }: Props) {
  const [userName, setUserName] = useState('');

  useEffect(() => {
    loadUserProfile();
  }, []);

  const loadUserProfile = async () => {
    try {
      const { data: userData } = await supabase.auth.getUser();
      if (userData.user) {
        const { data: profile } = await supabase
          .from('profiles')
          .select('full_name')
          .eq('id', userData.user.id)
          .single();

        if (profile) {
          setUserName(profile.full_name);
        }
      }
    } catch (error) {
      console.error('Profil yüklenemedi:', error);
    }
  };

  const handleLogout = async () => {
    Alert.alert('Çıkış', 'Çıkış yapmak istediğinize emin misiniz?', [
      { text: 'İptal', style: 'cancel' },
      {
        text: 'Çıkış Yap',
        style: 'destructive',
        onPress: async () => {
          await supabase.auth.signOut();
          navigation.replace('Login');
        },
      },
    ]);
  };

  return (
    <View style={styles.container}>
      {/* Animated Gradient Background */}
      <LinearGradient
        colors={['#E8D5F2', '#C8B6E2', '#A8C5E8', '#B8D5F2', '#D8C5E8']}
        locations={[0, 0.25, 0.5, 0.75, 1]}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 1 }}
        style={StyleSheet.absoluteFill}
      />

      {/* Light Streaks */}
      <View style={styles.lightStreak1}>
        <LinearGradient
          colors={['rgba(168, 197, 232, 0.3)', 'transparent']}
          start={{ x: 0, y: 0 }}
          end={{ x: 1, y: 1 }}
          style={styles.streakGradient}
        />
      </View>

      <View style={styles.lightStreak2}>
        <LinearGradient
          colors={['rgba(200, 182, 226, 0.25)', 'transparent']}
          start={{ x: 1, y: 0 }}
          end={{ x: 0, y: 1 }}
          style={styles.streakGradient}
        />
      </View>

      <SafeAreaView style={styles.safeArea}>
        {/* Floating Glass Header */}
        <BlurView intensity={35} tint="light" style={styles.headerBlur}>
          <LinearGradient
            colors={[
              'rgba(255, 255, 255, 0.3)',
              'rgba(255, 255, 255, 0.1)',
            ]}
            style={styles.headerGradient}
          >
            <View style={styles.headerContent}>
              <View>
                <Text style={styles.appName}>Ordevo</Text>
                {userName && (
                  <Text style={styles.welcomeText}>Hoş geldin, {userName}</Text>
                )}
              </View>
              <TouchableOpacity
                style={styles.logoutIconButton}
                onPress={handleLogout}
              >
                <BlurView intensity={20} tint="light" style={styles.logoutBlur}>
                  <Text style={styles.logoutIcon}>⎋</Text>
                </BlurView>
              </TouchableOpacity>
            </View>
          </LinearGradient>
          <View style={styles.headerBorder} />
        </BlurView>

        {/* Main Content */}
        <View style={styles.content}>
          {/* Tables Glass Panel */}
          <TouchableOpacity
            style={styles.panelContainer}
            onPress={() => navigation.navigate('Tables')}
            activeOpacity={0.85}
          >
            <BlurView intensity={40} tint="light" style={styles.panelBlur}>
              {/* Inner Glow */}
              <LinearGradient
                colors={[
                  'rgba(255, 255, 255, 0.35)',
                  'rgba(255, 255, 255, 0.08)',
                  'rgba(255, 255, 255, 0.25)',
                ]}
                locations={[0, 0.5, 1]}
                style={styles.panelInnerGlow}
              />

              {/* Outer Glow */}
              <View style={styles.panelOuterGlow} />

              <View style={styles.panelContent}>
                <View style={styles.iconContainer}>
                  <LinearGradient
                    colors={['#007AFF', '#0051D5']}
                    style={styles.iconGradient}
                  >
                    <Text style={styles.panelIcon}>⌘</Text>
                  </LinearGradient>
                </View>

                <Text style={styles.panelTitle}>Masalar</Text>
                <Text style={styles.panelDescription}>
                  Masa durumlarını görüntüle ve yönet
                </Text>

                <View style={styles.arrowContainer}>
                  <Text style={styles.arrow}>→</Text>
                </View>
              </View>

              {/* Melted Edges Effect */}
              <View style={styles.meltedEdge} />
            </BlurView>
          </TouchableOpacity>
        </View>

        {/* Footer */}
        <View style={styles.footer}>
          <Text style={styles.footerText}>Ordevo</Text>
        </View>
      </SafeAreaView>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  safeArea: {
    flex: 1,
  },
  lightStreak1: {
    position: 'absolute',
    top: -height * 0.2,
    right: -width * 0.3,
    width: width * 1.2,
    height: height * 0.6,
    transform: [{ rotate: '25deg' }],
  },
  lightStreak2: {
    position: 'absolute',
    bottom: -height * 0.2,
    left: -width * 0.3,
    width: width * 1.2,
    height: height * 0.5,
    transform: [{ rotate: '-15deg' }],
  },
  streakGradient: {
    flex: 1,
  },
  headerBlur: {
    marginHorizontal: 16,
    marginTop: 16,
    borderRadius: 24,
    overflow: 'hidden',
    backgroundColor: 'rgba(255, 255, 255, 0.12)',
    borderWidth: 1.5,
    borderColor: 'rgba(255, 255, 255, 0.25)',
  },
  headerGradient: {
    padding: 20,
  },
  headerBorder: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    borderRadius: 24,
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.3)',
    pointerEvents: 'none',
  },
  headerContent: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  appName: {
    fontSize: 28,
    fontWeight: '700',
    color: '#1A1A2E',
    letterSpacing: -0.5,
  },
  welcomeText: {
    fontSize: 14,
    fontWeight: '500',
    color: 'rgba(26, 26, 46, 0.6)',
    marginTop: 4,
  },
  logoutIconButton: {
    borderRadius: 12,
    overflow: 'hidden',
  },
  logoutBlur: {
    width: 44,
    height: 44,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: 'rgba(255, 255, 255, 0.15)',
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.25)',
  },
  logoutIcon: {
    fontSize: 20,
    color: '#1A1A2E',
  },
  content: {
    flex: 1,
    padding: 16,
    justifyContent: 'center',
  },
  panelContainer: {
    marginBottom: 20,
  },
  panelBlur: {
    borderRadius: 34,
    overflow: 'hidden',
    backgroundColor: 'rgba(255, 255, 255, 0.15)',
    borderWidth: 1.5,
    borderColor: 'rgba(255, 255, 255, 0.28)',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 20 },
    shadowOpacity: 0.15,
    shadowRadius: 30,
    elevation: 15,
  },
  panelInnerGlow: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    height: '60%',
    borderTopLeftRadius: 34,
    borderTopRightRadius: 34,
  },
  panelOuterGlow: {
    position: 'absolute',
    top: -2,
    left: -2,
    right: -2,
    bottom: -2,
    borderRadius: 36,
    backgroundColor: 'rgba(255, 255, 255, 0.08)',
    zIndex: -1,
  },
  meltedEdge: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    height: 40,
    backgroundColor: 'rgba(255, 255, 255, 0.05)',
    borderBottomLeftRadius: 34,
    borderBottomRightRadius: 34,
  },
  panelContent: {
    padding: 40,
    alignItems: 'center',
  },
  iconContainer: {
    marginBottom: 24,
    borderRadius: 24,
    overflow: 'hidden',
    shadowColor: '#007AFF',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.4,
    shadowRadius: 16,
    elevation: 10,
  },
  iconGradient: {
    width: 80,
    height: 80,
    borderRadius: 24,
    justifyContent: 'center',
    alignItems: 'center',
  },
  panelIcon: {
    fontSize: 40,
    color: '#FFFFFF',
  },
  panelTitle: {
    fontSize: 32,
    fontWeight: '700',
    color: '#1A1A2E',
    marginBottom: 12,
    letterSpacing: -0.5,
  },
  panelDescription: {
    fontSize: 16,
    fontWeight: '500',
    color: 'rgba(26, 26, 46, 0.6)',
    textAlign: 'center',
    lineHeight: 22,
  },
  arrowContainer: {
    marginTop: 24,
    width: 48,
    height: 48,
    borderRadius: 24,
    backgroundColor: 'rgba(0, 122, 255, 0.1)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  arrow: {
    fontSize: 24,
    color: '#007AFF',
    fontWeight: 'bold',
  },
  footer: {
    padding: 20,
    alignItems: 'center',
  },
  footerText: {
    fontSize: 13,
    fontWeight: '600',
    color: 'rgba(26, 26, 46, 0.4)',
    letterSpacing: 1,
  },
});
