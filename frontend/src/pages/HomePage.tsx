import { Link } from 'react-router-dom';
import { CheckCircle, Star, Clock, Shield, Sparkles, ArrowRight, Home, Calendar } from 'lucide-react';
import { Button, Card } from '@/components/ui';

export default function HomePage() {
  const features = [
    {
      icon: Clock,
      title: 'Book in 60 Seconds',
      description: 'Quick and easy online booking. Choose your date, time, and services.',
    },
    {
      icon: Shield,
      title: 'Trusted Professionals',
      description: 'Background-checked and trained cleaning experts you can trust.',
    },
    {
      icon: Star,
      title: '5-Star Service',
      description: '100% satisfaction guaranteed. We\'re not happy until you are.',
    },
    {
      icon: CheckCircle,
      title: 'Eco-Friendly',
      description: 'We use green cleaning products that are safe for your family.',
    },
  ];

  return (
    <div>
      {/* Welcome Banner */}
      <section className="relative overflow-hidden bg-gradient-to-br from-accent-50 via-accent-light/10 to-accent-50">
        <div className="absolute inset-0 overflow-hidden">
          <div className="absolute -top-20 -right-20 h-64 w-64 rounded-full bg-accent-light/15 blur-3xl" />
          <div className="absolute -bottom-20 -left-20 h-64 w-64 rounded-full bg-accent-light/10 blur-3xl" />
        </div>
        <div className="relative mx-auto max-w-3xl px-4 py-16 text-center sm:px-6 lg:px-8">
          <h1 className="text-3xl font-bold tracking-tight text-gray-900 sm:text-4xl lg:text-5xl">
            <Sparkles className="inline h-8 w-8 text-accent" />{' '}
            Welcome to a Cleaner, Fresher Space!{' '}
            <Sparkles className="inline h-8 w-8 text-accent" />
          </h1>
          <p className="mt-6 text-lg text-gray-700 leading-relaxed">
            We believe a clean space creates peace, comfort, and happiness. Our professional
            cleaning team is here to make your home or business shine while you relax and enjoy
            what matters most.
          </p>
          <div className="mt-8 flex flex-col items-center gap-4 sm:flex-row sm:justify-center sm:gap-8">
            <div className="flex items-center gap-2 text-lg text-gray-700">
              <CheckCircle className="h-5 w-5 text-green-500 flex-shrink-0" />
              Reliable service
            </div>
            <div className="flex items-center gap-2 text-lg text-gray-700">
              <Home className="h-5 w-5 text-brand flex-shrink-0" />
              Homes & offices
            </div>
            <div className="flex items-center gap-2 text-lg text-gray-700">
              <Sparkles className="h-5 w-5 text-accent flex-shrink-0" />
              Attention to every detail
            </div>
          </div>
          <p className="mt-8 text-lg text-gray-700">
            Let us take care of the cleaning so you can enjoy a spotless space every day!
          </p>
          <p className="mt-6 text-lg font-semibold text-brand">
            Contact us today to schedule your cleaning. Your sparkling space starts here!
          </p>

          <div className="mt-8 flex flex-col sm:flex-row gap-4 justify-center">
            <Link to="/booking">
              <Button size="lg" className="w-full sm:w-auto">
                Book Your Cleaning
                <ArrowRight className="ml-2 h-5 w-5" />
              </Button>
            </Link>
            <Link to="/services">
              <Button variant="outline" size="lg" className="w-full sm:w-auto">
                View Services
              </Button>
            </Link>
          </div>

          {/* Trust badges */}
          <div className="mt-6 flex items-center justify-center gap-1">
            {[...Array(5)].map((_, i) => (
              <Star key={i} className="h-5 w-5 fill-yellow-400 text-yellow-400" />
            ))}
            <span className="ml-2 text-sm font-medium text-gray-600">4.9/5 (2,000+ reviews)</span>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-20 bg-white">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-16">
            <div className="inline-flex items-center rounded-full bg-brand/5 px-4 py-1.5 text-sm font-medium text-brand mb-4">
              Why ecoMaid?
            </div>
            <h2 className="text-3xl font-bold text-gray-900 sm:text-4xl">
              Everything you need for a spotless space
            </h2>
            <p className="mt-4 text-lg text-gray-600 max-w-2xl mx-auto">
              We make professional cleaning easy, affordable, and stress-free.
            </p>
          </div>

          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
            {features.map((feature) => (
              <Card key={feature.title} hover className="p-6 text-center group">
                <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-xl bg-accent-light/20 group-hover:bg-accent-light/30 transition-colors">
                  <feature.icon className="h-7 w-7 text-brand" />
                </div>
                <h3 className="text-lg font-semibold text-gray-900">{feature.title}</h3>
                <p className="mt-2 text-sm text-gray-600 leading-relaxed">{feature.description}</p>
              </Card>
            ))}
          </div>
        </div>
      </section>

      {/* How It Works */}
      <section className="py-20 bg-gradient-to-b from-gray-50 to-white">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-16">
            <div className="inline-flex items-center rounded-full bg-accent-light/20 px-4 py-1.5 text-sm font-medium text-brand-dark mb-4">
              Simple process
            </div>
            <h2 className="text-3xl font-bold text-gray-900 sm:text-4xl">
              How It Works
            </h2>
            <p className="mt-4 text-lg text-gray-600">
              Getting your home cleaned is as easy as 1-2-3
            </p>
          </div>

          <div className="grid gap-8 md:grid-cols-3">
            {[
              { step: '1', title: 'Book Online', description: 'Choose your service, date, and time in our easy booking system.', icon: Calendar },
              { step: '2', title: 'We Clean', description: 'Our professional cleaners arrive on time and work their magic.', icon: Sparkles },
              { step: '3', title: 'Relax & Enjoy', description: 'Come home to a sparkling clean space. It\'s that simple!', icon: CheckCircle },
            ].map((item, index) => (
              <div key={item.step} className="relative text-center group">
                {/* Connector line between steps */}
                {index < 2 && (
                  <div className="hidden md:block absolute top-8 left-[60%] w-[80%] h-px bg-gradient-to-r from-brand/20 to-brand/20" />
                )}
                <div className="relative mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-2xl bg-brand text-white shadow-lg shadow-brand/20 group-hover:shadow-xl group-hover:shadow-brand/30 transition-shadow">
                  <item.icon className="h-7 w-7" />
                  <span className="absolute -top-2 -right-2 flex h-6 w-6 items-center justify-center rounded-full bg-accent-light text-xs font-bold text-brand">
                    {item.step}
                  </span>
                </div>
                <h3 className="text-xl font-semibold text-gray-900">{item.title}</h3>
                <p className="mt-2 text-gray-600 max-w-xs mx-auto">{item.description}</p>
              </div>
            ))}
          </div>

          <div className="mt-12 text-center">
            <Link to="/booking">
              <Button size="lg">
                Get Started Now
                <ArrowRight className="ml-2 h-5 w-5" />
              </Button>
            </Link>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="relative py-20 bg-brand overflow-hidden">
        <div className="absolute inset-0">
          <div className="absolute -top-40 -right-40 h-80 w-80 rounded-full bg-accent-light/10 blur-3xl" />
          <div className="absolute -bottom-40 -left-40 h-80 w-80 rounded-full bg-white/5 blur-3xl" />
        </div>
        <div className="relative mx-auto max-w-4xl px-4 text-center sm:px-6 lg:px-8">
          <h2 className="text-3xl font-bold text-white sm:text-4xl">
            Ready for a Cleaner Home?
          </h2>
          <p className="mt-4 text-lg text-sky-100 max-w-2xl mx-auto">
            Book your professional cleaning today and enjoy a spotless space tomorrow.
          </p>
          <div className="mt-8 flex flex-col sm:flex-row gap-4 justify-center">
            <Link to="/booking">
              <Button size="lg" variant="secondary" className="bg-accent-light text-brand hover:bg-accent font-semibold">
                Book Now - It's Free to Schedule
                <ArrowRight className="ml-2 h-5 w-5" />
              </Button>
            </Link>
            <Link to="/contact">
              <Button size="lg" variant="outline" className="border-white/30 text-white hover:bg-white/10">
                Contact Us
              </Button>
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
